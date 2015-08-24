// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents an interactive code entry point that is inserted into the compilation if there is not an existing one. 
    /// </summary>
    internal sealed class SynthesizedConstantHelperMethod : SynthesizedStaticMethodBaseForRecords
    {
        private readonly ImmutableArray<ParameterSymbol> parameters;
        private readonly TypeSymbol returnType;

        internal SynthesizedConstantHelperMethod(string name, NamedTypeSymbol containingType, TypeSymbol param1Type, TypeSymbol param2Type, TypeSymbol returnType, TypeCompilationState compilationState, DiagnosticBag diagnostics) :
            base(containingType, name)
        {
            Debug.Assert((object)containingType != null);
            this.returnType = returnType;
            this.parameters = ImmutableArray.Create<ParameterSymbol>(new SynthesizedParameterSymbol(this, param1Type, 0, RefKind.None, "o"), new SynthesizedParameterSymbol(this, param2Type, 1, RefKind.None, "v"));

            GenerateMethodBody(compilationState, diagnostics);
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = CreateBoundNodeFactory(compilationState, diagnostics);

            try
            {
                BoundBlock body;
                var obj = F.Parameter(this.Parameters[0]);
                var val = F.Parameter(this.Parameters[1]);
                switch (this.Name)
                {
                    // TODO: It is not complete. It needs other helpers for uint32, int64, etc.
                    // It can be optimized a lot! The current one can be served as a template. 
                    case "<>Int32Helper":
                        {
                            body = F.Block(
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_Byte)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_Byte), obj), val))),                                    
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_SByte)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_SByte), obj), val))),
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_Int16)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_Int16), obj), val))),
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_UInt16)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_UInt16), obj), val))),
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_Int32)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_Int32), obj), val))),
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_UInt32)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_UInt32), obj), val))),
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_Int64)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_Int64), obj), F.Cast(F.SpecialType(SpecialType.System_Int64), val)))),
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_UInt64)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_UInt64), obj), F.Cast(F.SpecialType(SpecialType.System_UInt64), val)))),
                            F.Return(F.Literal(false)));
                            break;
                        }
                    case "<>UInt32Helper":
                        {
                            body = F.Block(
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_UInt32)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_UInt32), obj), val))),
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_Int64)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_Int64), obj), val))),
                            F.Return(F.Literal(false)));
                            break;
                        }
                    case "<>DoubleHelper":
                        {
                            body = F.Block(
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_Double)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_Double), obj), val))),
                            F.If(
                                F.Is(obj, F.SpecialType(SpecialType.System_Single)),
                                F.Return(F.ObjectEqual(F.Cast(F.SpecialType(SpecialType.System_Single), obj), val))),
                            F.Return(F.Literal(false)));
                            break;
                        }
                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.Name);
                }
                F.CloseMethod(body);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Private; }
        }

        public override TypeSymbol ReturnType
        {
            get { return returnType; }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.Ordinary; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return parameters; }
        }
    }
}
