// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{

    internal abstract class SynthesizedInstanceMethodBaseForRecords : SynthesizedMethodBaseForRecords
    {
        private ParameterSymbol lazyThisParameter;

        public SynthesizedInstanceMethodBaseForRecords(NamedTypeSymbol containingType, string name) :
            base(containingType, name)
        { }

        public sealed override bool IsStatic
        {
            get { return false; }
        }

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            if ((object)lazyThisParameter == null)
            {
                Interlocked.CompareExchange(ref lazyThisParameter, new ThisParameterSymbol(this), null);
            }

            thisParameter = lazyThisParameter;
            return true;
        }

        internal sealed override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return Microsoft.Cci.CallingConvention.HasThis; }
        }
    }

    internal abstract class SynthesizedStaticMethodBaseForRecords : SynthesizedMethodBaseForRecords
    {
        public SynthesizedStaticMethodBaseForRecords(NamedTypeSymbol containingType, string name) :
            base(containingType, name)
        { }

        internal sealed override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return 0; }
        }

        public sealed override bool IsStatic
        {
            get { return true; }
        }
    }

    internal abstract class SynthesizedMethodBaseForRecords : MethodSymbol
    {
        private readonly NamedTypeSymbol containingType;
        private readonly string name;

        public SynthesizedMethodBaseForRecords(NamedTypeSymbol containingType, string name)
        {
            this.containingType = containingType;
            this.name = name;
        }

        internal sealed override bool GenerateDebugInfo
        {
            get { return false; }
        }

        public sealed override int Arity
        {
            get { return 0; }
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return this.containingType; }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
            }
        }

        public override bool ReturnsVoid
        {
            get { return this.ReturnType.SpecialType == SpecialType.System_Void; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public abstract override Accessibility DeclaredAccessibility
        {
            get;
        }

        public sealed override bool IsVirtual
        {
            get { return false; }
        }

        public sealed override bool IsAsync
        {
            get { return false; }
        }

        internal sealed override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        public sealed override bool IsExtensionMethod
        {
            get { return false; }
        }

        public sealed override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public sealed override bool IsVararg
        {
            get { return false; }
        }

        public sealed override ImmutableArray<TypeSymbol> TypeArguments
        {
            get { return ImmutableArray<TypeSymbol>.Empty; }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        internal sealed override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        public sealed override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public sealed override bool IsAbstract
        {
            get { return false; }
        }

        public sealed override bool IsSealed
        {
            get { return false; }
        }

        public sealed override bool IsExtern
        {
            get { return false; }
        }

        public sealed override string Name
        {
            get { return this.name; }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal sealed override bool RequiresSecurityObject
        {
            get { return false; }
        }

        public sealed override DllImportData GetDllImportData()
        {
            return null;
        }

        internal sealed override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override bool SynthesizesLoweredBoundBody
        {
            get
            {
                return true;
            }
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataFinal()
        {
            return false;
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return ImmutableArray<ParameterSymbol>.Empty; }
        }

        protected SyntheticBoundNodeFactory CreateBoundNodeFactory(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = this;
            return F;
        }

        public abstract override bool IsOverride
        {
            get;
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return LexicalSortKey.NotInSource;
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get { return true; }
        }
    }
}
