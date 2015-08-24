// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitMatchExpression(BoundMatchExpression node)
        {
            BoundExpression loweredOperand = VisitExpression(node.Operand);
            BoundPattern pattern = node.Pattern;
            var F = this.factory;

            switch (pattern.Kind)
            {
                case BoundKind.WildCardPattern:
                    {
                        return F.Literal(true);
                    }

                case BoundKind.ConstantPattern:
                    {
                        var constantPattern = (BoundConstantPattern)pattern;
                        var constant = constantPattern.BoundConstant;

                        return CompareWithConstant(loweredOperand, constant);
                    }

                case BoundKind.DeclarationPattern:
                    {
                        // TODO: Support for nullable types. If there is a nullable type, rewrite it to: (operand.HasValue && (local = (type) operand.GetValue)) 
                        var declarationPattern = (BoundDeclarationPattern)pattern;
                        var local = declarationPattern.LocalSymbol;
                        var type = local.Type;
                        // we cannot use as operator with primitive types.
                        // operand is type && (local = (type)operand) != null

                        BoundExpression rewritten;
                        if (loweredOperand.Type.IsNullableType())
                        {
                            rewritten = F.Property(loweredOperand, "HasValue");
                            loweredOperand = F.Call(loweredOperand, loweredOperand.Type, "GetValueOrDefault", ImmutableArray<BoundExpression>.Empty);
                        }
                        else
                        {
                            rewritten = F.Is(loweredOperand, type);
                        }
                        
                        rewritten = F.LogicalAnd(rewritten,
                                                 F.Is(F.AssignmentExpression(F.Local(local), F.Cast(type, loweredOperand)), type));
                        //if (type.IsValueType)
                        //{
                        //    // if it already passed the type checking in the binder, it should be converted. 
                        //    // (local = (type)operand) is type . 
                        //    newExpression = F.ObjectNotEqual(F.As(F.AssignmentExpression(F.Local(local), F.Cast(type, loweredOperand)), type, Conversion.ExplicitNumeric),
                        //                                     F.Null(type));
                        //} 

                        //    F.LogicalAnd(F.Is(loweredOperand, type),
                        //                               F.IntNotEqual(F.AssignmentExpression(F.Local(local), F.Cast(type, loweredOperand)),
                        //                                                F.Null(type)));

                        // operand is type && (local = (type)operand) is type

                        return rewritten;
                    }

                case BoundKind.RecursivePattern:
                    return RewriteRecursivePattern((BoundRecursivePattern)pattern, loweredOperand);

                case BoundKind.PropertyPattern:
                    return RewritePropertyPattern((BoundPropertyPattern)pattern, loweredOperand);
                default:
                    return null;
            }
        }

        private BoundNode RewriteRecursivePattern(BoundRecursivePattern boundRecursivePattern, BoundExpression operand)
        {
            var F = this.factory;

            TypeSymbol type = boundRecursivePattern.Type;
            ImmutableArray<int> patternsToParams = boundRecursivePattern.PatternsToParams;
            ImmutableArray<BoundPattern> patterns = boundRecursivePattern.Patterns;

            var isOperator = boundRecursivePattern.IsOperator;
            var isOperatorParameterTypes = isOperator.ParameterTypes;

            BoundExpression[] isOperatorCallArguments = new BoundExpression[isOperatorParameterTypes.Length];
            RefKind[] isOperatorCallArgumentsRefKinds = new RefKind[isOperatorParameterTypes.Length];

            // operand is Add(...) => operand is Add && Add.matches((Add)operand, ...)
            BoundExpression current = F.Is(operand, isOperatorParameterTypes[0]);

            SmallDictionary<BoundPattern, LocalSymbol> synthesizedLocalsMap = new SmallDictionary<BoundPattern, LocalSymbol>();

            isOperatorCallArguments[0] = F.Cast(isOperatorParameterTypes[0], operand);
            isOperatorCallArgumentsRefKinds[0] = RefKind.None;

            Debug.Assert((isOperatorCallArguments.Length - 1) == patterns.Length);

            // Adding arguments to the isOperatorCall based on the patternsToParamsOpt
            for (int i = 0; i < patterns.Length; i++)
            {
                var pattern = patterns[i];
                var paramIndex = patternsToParams[i];
                var paramType = isOperatorParameterTypes[paramIndex];
                LocalSymbol local = F.SynthesizedLocal(paramType);
                synthesizedLocalsMap.Add(pattern, local);

                isOperatorCallArguments[paramIndex] = F.Local(local);
                isOperatorCallArgumentsRefKinds[paramIndex] = RefKind.Out;
            }

            var isOperatorCall = F.Call(null, isOperator, isOperatorCallArguments.AsImmutable(), isOperatorCallArgumentsRefKinds.AsImmutable());

            current = F.Binary(BinaryOperatorKind.LogicalAnd, F.SpecialType(SpecialType.System_Boolean),
                current,
                isOperatorCall);


            foreach (KeyValuePair<BoundPattern, LocalSymbol> entry in synthesizedLocalsMap)
            {
                BoundPattern pattern = entry.Key;
                LocalSymbol synLocal = entry.Value;

                BoundExpression newBoundExpression = (BoundExpression)Visit(F.Match(F.Local(synLocal), pattern));

                if (newBoundExpression != null)
                    current = F.Binary(BinaryOperatorKind.LogicalAnd, F.SpecialType(SpecialType.System_Boolean),
                                        current, newBoundExpression);
            }

            return F.Sequence(synthesizedLocalsMap.Values.AsImmutable(), current);
        }

        private BoundNode RewritePropertyPattern(BoundPropertyPattern boundPropertyPattern, BoundExpression operand)
        {
            var F = this.factory;
            var type = boundPropertyPattern.Type;
            var patterns = boundPropertyPattern.Patterns;
            var properties = boundPropertyPattern.Properties;

            BoundExpression current = F.Is(operand, type);
            operand = F.Cast(type, operand);

            for (int i = 0; i < patterns.Length; i++)
            {
                var pattern = patterns[i];
                var property = properties[i];
                BoundExpression newOperand = F.Call(operand, property.GetMethod);

                var temp = (BoundExpression)Visit(F.Match(newOperand, pattern));
                current = F.LogicalAnd(current, temp);
            }
            return current;
        }

        private BoundExpression CompareWithConstant(BoundExpression operand, BoundExpression constant)
        {
            var F = this.factory;

            // operand is null => (object)operand == null
            if (constant.Type == null)
            {
                return F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_Object), operand), constant);
            }
            else if (ConversionsBase.IsNumericType(constant.Type.SpecialType))
            {
                if (operand.Type.SpecialType == SpecialType.System_Object)
                {
                    var helperMethod = NumericConstantHelperMethodManager(operand, constant);
                    var arguments = ImmutableArray.Create<BoundExpression>(operand, constant);

                    var call = F.Call(null, helperMethod, arguments);
                    return call;
                }
                else if (operand.Type.IsNullableType())
                {
                    var temp = F.Property(operand, "HasValue");
                    operand = F.Call(operand, operand.Type, "GetValueOrDefault", ImmutableArray<BoundExpression>.Empty);
                    return F.LogicalAnd(temp, F.ObjectEqual(operand, constant));
                }
                return F.ObjectEqual(operand, constant);
            }
            else
            {
                if (operand.Type.IsNullableType())
                {
                    var temp = F.Property(operand, "HasValue");
                    operand = F.Call(operand, operand.Type, "GetValueOrDefault", ImmutableArray<BoundExpression>.Empty);
                    return F.LogicalAnd(temp, F.ObjectEqual(F.Cast(constant.Type, operand), constant));
                }
                return F.LogicalAnd(F.Is(operand, constant.Type),
                                    F.ObjectEqual(F.Cast(constant.Type, operand), constant));
            }
        }

        private MethodSymbol NumericConstantHelperMethodManager(BoundExpression left, BoundExpression constant)
        {
            var f = this.factory;

            var classSymbol = f.CurrentClass;
            string methodName = HelperMethodName(constant.Type.SpecialType);

            var symbols = f.CompilationState.ModuleBuilderOpt.GetSynthesizedMethods(classSymbol);

            if (symbols != null)
            {
                foreach (var symbol in symbols)
                {
                    if (symbol.Name == methodName)
                        return (MethodSymbol)symbol;
                }
            }

            TypeSymbol boolType = this.compilation.GetSpecialType(SpecialType.System_Boolean);
            var helperMethod = new SynthesizedConstantHelperMethod(methodName, classSymbol, left.Type, constant.Type, boolType, f.CompilationState, diagnostics);

            //  add the method to module
            if (f.CompilationState.Emitting)
            {
                f.CompilationState.ModuleBuilderOpt.AddSynthesizedDefinition(classSymbol, helperMethod);
            }
            return helperMethod;

        }

        private static string HelperMethodName(SpecialType type)
        {
            switch (type)
            {
                case SpecialType.System_Int32:
                    return "<>Int32Helper";
                case SpecialType.System_UInt32:
                    return "<>UInt32Helper";
                case SpecialType.System_Int64:
                    return "<>Int64Helper";
                case SpecialType.System_UInt64:
                    return "<>UInt64Helper";
                case SpecialType.System_Double:
                    return "<>DoubleHelper";
                case SpecialType.System_Single:
                    return "<>FloatHelper";
                case SpecialType.System_Decimal:
                    return "<>DecimalHelper";
                default:
                    throw ExceptionUtilities.UnexpectedValue(type);
            }
        }

    }
}
