// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts PatternSyntax nodes into BoundPattern nodes.
    /// </summary>
    internal partial class Binder
    {
        internal BoundPattern BindPattern(PatternSyntax node, TypeSymbol operandType, DiagnosticBag diagnostics)
        {
            var operandTypeCandidates = PooledHashSet<TypeSymbol>.GetInstance();
            operandTypeCandidates.Add(operandType);
            var boundPattern = BindPattern(node, operandTypeCandidates, diagnostics);
            operandTypeCandidates.Free();
            return boundPattern;
        }

        // One of 'operandType' or 'opIsCandidates' must be null. There are two modes:
        // (1) You bind a pattern which is not in the recursive pattern. In this case you just have one candidate for the operand type.
        // (2) You bind a pattern which in in the recursive pattern. In this case, you might have many candidates for the operand type because of the parent recursive pattern's multiple op_Is methods. 
        internal BoundPattern BindPattern(PatternSyntax node, HashSet<TypeSymbol> operandTypeCandidates, DiagnosticBag diagnostics)
        {
            switch (node.Kind)
            {
                case SyntaxKind.WildCardPattern:
                    return new BoundWildCardPattern(node);

                case SyntaxKind.ConstantPattern:
                    {
                        var constantPattern = (ConstantPatternSyntax)node;
                        return BindConstantPattern(constantPattern.Expression, operandTypeCandidates, diagnostics);
                    }

                case SyntaxKind.DeclarationPattern:
                    {
                        var declarationPattern = (DeclarationPatternSyntax)node;
                        var typeSyntax = declarationPattern.Type;
                        var localSymbol = LookupLocal(declarationPattern.Identifier);

                        if (!typeSyntax.IsVar)
                        {
                            // Source: operandType, destination: localSymbol.type
                            RemoveBadTypeCandidates(declarationPattern, localSymbol.Type, operandTypeCandidates, diagnostics);
                        }
                        else
                        {
                            if (operandTypeCandidates.Count == 1)
                            {
                                localSymbol.SetTypeSymbol(operandTypeCandidates.ToImmutableList()[0]);
                            }
                            // I have to set the type of the var declarations after op_Is is found if there is more than one operandTypeCandidate
                        }

                        return new BoundDeclarationPattern(node, localSymbol);
                    }
                case SyntaxKind.RecursivePattern:
                    return BindRecursivePattern((RecursivePatternSyntax)node, operandTypeCandidates, diagnostics);

                case SyntaxKind.PropertyPattern:
                    return BindPropertyPattern((PropertyPatternSyntax)node, operandTypeCandidates, diagnostics);

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind);
            }
        }

        internal BoundPattern BindConstantPattern(ExpressionSyntax constantExpr, TypeSymbol operandType, DiagnosticBag diagnostics)
        {
            var operandTypeCandidates = PooledHashSet<TypeSymbol>.GetInstance();
            operandTypeCandidates.Add(operandType);
            var boundPattern = BindConstantPattern(constantExpr, operandTypeCandidates, diagnostics);
            operandTypeCandidates.Free();
            return boundPattern;
        }

        internal BoundPattern BindConstantPattern(ExpressionSyntax constantExpr, HashSet<TypeSymbol> operandTypeCandidates, DiagnosticBag diagnostics)
        {
            bool hasErrors;
            var boundConstant = BindValue(constantExpr, diagnostics, BindValueKind.RValue);
            hasErrors = boundConstant.HasAnyErrors;

            if (!hasErrors)
            {
                if (boundConstant.ConstantValue == null)
                {
                    Error(diagnostics, ErrorCode.ERR_ConstantExpected, constantExpr);
                    hasErrors = true;
                }
                // Check whether it is not 'null' type.
                else if (boundConstant.Type != null)
                {
                    // source: boundConstantType, destination: operandTypes
                    RemoveBadTypeCandidates(constantExpr, boundConstant.Type, operandTypeCandidates, diagnostics);
                }
            }
            return new BoundConstantPattern(constantExpr, boundConstant, hasErrors);
        }

        private BoundPattern BindRecursivePattern(RecursivePatternSyntax node, HashSet<TypeSymbol> operandTypeCandidates, DiagnosticBag diagnostics)
        {
            var type = (NamedTypeSymbol)this.BindType(node.Type, diagnostics);
            var subPatterns = node.PatternList.SubPatterns;
            var opIsCandidates = ArrayBuilder<MethodSymbol>.GetInstance();
            // First, add all op_Is methods in the type
            foreach (var member in type.GetMembers(WellKnownMemberNames.IsOperatorName))
            {
                if (member.Kind == SymbolKind.Method)
                {
                    opIsCandidates.Add((MethodSymbol)member);
                }
            }

            // Remove the candidates which have different number of operands or do not have the paramName in the subpattern.
            RemoveBadParamsCandidates(opIsCandidates, subPatterns);

            // If there is no opIs, don't bind the subpatterns!
            if (opIsCandidates.Count == 0)
            {
                // TODO Add an error message saying that "There is no suitable op_Is method"
                Error(diagnostics, ErrorCode.ERR_NoSuchMember, node, type, "is operator");
                opIsCandidates.Free();
                return new BoundWildCardPattern(node, hasErrors: true);
            }

            // Check whether all patterns have a name after a named pattern.
            bool isAnyNamedPattern = false;
            foreach (var subPattern in subPatterns)
            {
                if (subPattern.NameColon != null)
                {
                    isAnyNamedPattern = true;
                }
                else if (isAnyNamedPattern)
                {
                    Error(diagnostics, ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, subPattern);
                    return new BoundWildCardPattern(node, hasErrors: true);
                }
            }

            ImmutableArray<BoundPattern> boundPatterns = BindSubRecursivePatterns(node, opIsCandidates, diagnostics);

            // Second, eliminate opIsCandidates if there is no operandType candidate that can be converted to it.
            // Also, eliminate operandTypeCandidates that cannot be converted to the first parameter type of the opIsCandidate, if there are other working operandTypeCandidates!!!
            Unify(node.Type, operandTypeCandidates, opIsCandidates, diagnostics);

            MethodSymbol opIs = null;
            ImmutableArray<int> patternsToParams;
            bool hasErrors = false;
            if (opIsCandidates.Count > 0)
            {
                // If there are more than one opIsCandidate, choose the first one.
                opIs = opIsCandidates[0];

                // Constructing patternsToParams, an integer array which specifies the order of the patterns based on the opIs parameters.
                patternsToParams = ConstructPatterns2Params(node, opIs);

                // Set the type of 'var' declarations based on the opIs parameters
                SetTypeForVarDeclarations(subPatterns, boundPatterns, opIs);
            }
            else
            {
                patternsToParams = ImmutableArray<int>.Empty;
                hasErrors = true;
            }
            opIsCandidates.Free();
            return new BoundRecursivePattern(node, type, opIs, boundPatterns, patternsToParams, hasErrors);
        }

        private BoundPattern BindPropertyPattern(PropertyPatternSyntax node, HashSet<TypeSymbol> operandTypeCandidates, DiagnosticBag diagnostics)
        {
            var type = (NamedTypeSymbol)this.BindType(node.Type, diagnostics);
            var properties = ArrayBuilder<PropertySymbol>.GetInstance();
            var boundPatterns = BindSubPropertyPatterns(node, properties, type, diagnostics);
            bool hasErrors = properties.Count != boundPatterns.Length;
            return new BoundPropertyPattern(node, type, boundPatterns, properties.ToImmutableAndFree(), hasErrors: hasErrors);
        }

        // operandTypeCandidates and opIsCandidates might be modified.
        private void Unify(CSharpSyntaxNode node, HashSet<TypeSymbol> operandTypeCandidates, ArrayBuilder<MethodSymbol> opIsCandidates, DiagnosticBag diagnostics)
        {
            for (int j = opIsCandidates.Count - 1; j >= 0; j--)
            {
                var firstParamType = opIsCandidates[j].ParameterTypes[0];
                var isAnyGoodOperandCandidate = false;

                // This is going to be used to diagnose the problem if there is no good operand candidate and there is only one opIsCandidate left. 
                TypeSymbol oneOperandType = null ;
                foreach (var operandType in operandTypeCandidates)
                {
                    oneOperandType = operandType;
                    if (IsAnyConversionForPattern(node, firstParamType, operandType, diagnostics, isDiagnosed: false))
                    {
                        isAnyGoodOperandCandidate = true;
                    }
                }
                if (!isAnyGoodOperandCandidate)
                {
                    // If there is only one opIsCandidate left, diagnose the problem by saying that there is no conversion from x type to y type.
                    if (opIsCandidates.Count == 1)
                    {
                        // operandTypeCandidates cannot be empty!
                        Debug.Assert(oneOperandType != null);
                        IsAnyConversionForPattern(node, firstParamType, oneOperandType, diagnostics, isDiagnosed: true);
                    }
                    opIsCandidates.RemoveAt(j);
                }
                else
                {
                    RemoveBadTypeCandidates(node, firstParamType, operandTypeCandidates, diagnostics);
                }
            }
        }

        private ImmutableArray<BoundPattern> BindSubRecursivePatterns(RecursivePatternSyntax node, ArrayBuilder<MethodSymbol> opIsCandidates, DiagnosticBag diagnostics)
        {
            ArrayBuilder<BoundPattern> boundPatternsBuilder = ArrayBuilder<BoundPattern>.GetInstance();

            // Patterns start after the operand in the parameter list of op_Is:
            // operand is Type(subpatterns) => Type.op_Is(operand, subpatterns)
            // That's why, it starts from 1
            int paramIndex = 1;
            var operandTypeCandidates = PooledHashSet<TypeSymbol>.GetInstance();
            foreach (var syntax in node.PatternList.SubPatterns)
            {
                // We guarantee that there is no candidate which has a paramater using paramName if paramName is not empty
                ChooseOperandTypeCandidates(operandTypeCandidates, opIsCandidates, paramIndex, syntax.NameColon); // every opIsCandidate might have this paramName in different indices.

                BoundPattern boundPattern = this.BindPattern(syntax.Pattern, operandTypeCandidates, diagnostics);

                // Remove the opIsCandidates which do not use one of the operandTypeCandidates in the correct index. 
                UpdateOpIsCandidates(opIsCandidates, operandTypeCandidates, paramIndex, syntax.NameColon);

                boundPatternsBuilder.Add(boundPattern);
                operandTypeCandidates.Clear();
                paramIndex++;
            }
            operandTypeCandidates.Free();

            return boundPatternsBuilder.ToImmutableAndFree();
        }

        private ImmutableArray<BoundPattern> BindSubPropertyPatterns(PropertyPatternSyntax node, ArrayBuilder<PropertySymbol> properties, TypeSymbol type, DiagnosticBag diagnostics)
        {
            var boundPatternsBuilder = ArrayBuilder<BoundPattern>.GetInstance();
            foreach (var syntax in node.PatternList.SubPatterns)
            {
                var propName = syntax.Left;
                BoundPattern pattern;

                PropertySymbol property = FindPropertyByName(type, propName);
                if ((object)property != null)
                {
                    properties.Add(property);
                    pattern = this.BindPattern(syntax.Pattern, property.Type, diagnostics);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_NoSuchMember, propName, type, propName.Identifier.ValueText);
                    pattern = new BoundWildCardPattern(node, hasErrors: true);
                }
                boundPatternsBuilder.Add(pattern);
            }
            return boundPatternsBuilder.ToImmutableAndFree();
        }

        private void RemoveBadTypeCandidates(CSharpSyntaxNode node, TypeSymbol type, HashSet<TypeSymbol> operandTypeCandidates, DiagnosticBag diagnostics)
        {
            // Only if there is only one candidate left, we should diagnose the issue.
            operandTypeCandidates.RemoveWhere((operandType) =>
                !IsAnyConversionForPattern(node, type, operandType, diagnostics, isDiagnosed: operandTypeCandidates.Count == 1));
        }

        private bool IsAnyConversionForPattern(CSharpSyntaxNode node, TypeSymbol source, TypeSymbol destination, DiagnosticBag diagnostics, bool isDiagnosed)
        {
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            bool isAnyConversion = Conversions.ClassifyImplicitConversion(source, destination, ref useSiteDiagnostics).Exists;

            if (!isAnyConversion)
            {
                // if the only candidate is this one, report the error
                if (isDiagnosed)
                {
                    SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, source, destination);
                    Error(diagnostics, ErrorCode.ERR_NoImplicitConv, node, distinguisher.First, distinguisher.Second);
                }
            }
            return isAnyConversion;
        }

        private static void SetTypeForVarDeclarations(SeparatedSyntaxList<SubRecursivePatternSyntax> subPatterns, ImmutableArray<BoundPattern> boundPatterns, MethodSymbol opIs)
        {
            for (int i = 0; i < subPatterns.Count; i++)
            {
                if (subPatterns[i].Pattern.Kind == SyntaxKind.DeclarationPattern)
                {
                    var declarationPattern = (DeclarationPatternSyntax)subPatterns[i].Pattern;
                    if (declarationPattern.Type.IsVar)
                    {
                        var local = ((BoundDeclarationPattern)boundPatterns[i]).LocalSymbol;
                        Debug.Assert(local is SourceLocalSymbol);
                        // Choose the param type from the OpIs. the param index should be i+1 because of the shifting.
                        var type = opIs.ParameterTypes[FindParamIndex(opIs, subPatterns[i].NameColon, i + 1)];
                        ((SourceLocalSymbol)local).SetTypeSymbol(type);
                    }
                }
            }
        }

        private static void ChooseOperandTypeCandidates(HashSet<TypeSymbol> operandTypeCandidates, ArrayBuilder<MethodSymbol> opIsCandidates, int paramIndex, NameColonSyntax nameColon)
        {
            foreach (var opIs in opIsCandidates)
            {
                TypeSymbol operandType = opIs.ParameterTypes[FindParamIndex(opIs, nameColon, paramIndex)];
                operandTypeCandidates.Add(operandType);
            }
        }

        private static void UpdateOpIsCandidates(ArrayBuilder<MethodSymbol> opIsCandidates, HashSet<TypeSymbol> operandTypeCandidates, int paramIndex, NameColonSyntax nameColon)
        {
            for (int i = opIsCandidates.Count - 1; i >= 0; i--)
            {
                var opIs = opIsCandidates[i];
                TypeSymbol operandType = opIs.ParameterTypes[FindParamIndex(opIs, nameColon, paramIndex)];
                if (!operandTypeCandidates.Contains(operandType))
                {
                    opIsCandidates.RemoveAt(i);
                }
            }
        }

        private static ImmutableArray<int> ConstructPatterns2Params(RecursivePatternSyntax node, MethodSymbol opIs)
        {
            ArrayBuilder<int> patternsToParams = ArrayBuilder<int>.GetInstance();
            int paramIndex = 1;
            foreach (var syntax in node.PatternList.SubPatterns)
            {
                var patternSyntax = syntax.Pattern;
                var nameColon = syntax.NameColon;

                // We already elimated the methods which do not use this  name
                patternsToParams.Add(FindParamIndex(opIs, nameColon, paramIndex));

                paramIndex++;
            }
            return patternsToParams.ToImmutableAndFree();
        }

        // Remove the opIsCandidates which have wrong arity and which do not use a paramName that subPattern has.
        private static void RemoveBadParamsCandidates(ArrayBuilder<MethodSymbol> opIsCandidates, SeparatedSyntaxList<SubRecursivePatternSyntax> subPatterns)
        {
            for (int i = opIsCandidates.Count - 1; i >= 0; i--)
            {
                var opIs = opIsCandidates[i];
                // Lets decrease the parameter count by 1, because it also has the operand besides the patterns
                if (opIs.ParameterCount - 1 != subPatterns.Count)
                {
                    opIsCandidates.RemoveAt(i);
                }
                else
                {
                    // if there is no problem with the number of the patterns, check the paramName.
                    foreach (var subPattern in subPatterns)
                    {
                        if (FindParamIndex(opIs, subPattern.NameColon) < 0)
                        {
                            opIsCandidates.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private static int FindParamIndex(MethodSymbol method, NameColonSyntax nameColon, int index = 0)
        {
            if (nameColon == null)
            {
                return index;
            }
            var name = nameColon.Name.Identifier.ValueText;
            var parameters = method.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        private PropertySymbol FindPropertyByName(TypeSymbol type, IdentifierNameSyntax name)
        {
            var symbols = ArrayBuilder<Symbol>.GetInstance();
            var result = LookupResult.GetInstance();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.LookupMembersWithFallback(result, type, name.Identifier.ValueText, 0, ref useSiteDiagnostics);

            if (result.IsMultiViable)
            {
                foreach (var symbol in result.Symbols)
                {
                    if (symbol.Kind == SymbolKind.Property)
                    {
                        return (PropertySymbol)symbol;
                    }
                }
            }
            return null;
        }
    }
}
