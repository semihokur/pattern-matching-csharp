// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{

    internal sealed class SynthesizedPropertySymbol : PropertySymbol
    {
        private readonly NamedTypeSymbol containingType;
        private readonly TypeSymbol type;
        private readonly string name;
        private readonly Location location;
        private readonly SynthesizedPropertyGetAccessorSymbol getMethod;
        private readonly FieldSymbol backingField;

        internal SynthesizedPropertySymbol(string name, Location location, NamedTypeSymbol containingType, TypeSymbol type)
        {
            this.containingType = containingType;
            this.type = type;
            this.name = name;
            this.location = location;

            this.backingField = new SynthesizedBackingFieldSymbol(this,
                                                      name: GeneratedNames.MakeBackingFieldName(this.name),
                                                      isReadOnly: true,
                                                      isStatic: false,
                                                      hasInitializer: true);
           
           this.getMethod = new SynthesizedPropertyGetAccessorSymbol(this);
        }

        public override TypeSymbol Type
        {
            get { return this.type; }
        }

        public override string Name
        {
            get { return this.name; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create(location);
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return GetDeclaringSyntaxReferenceHelper<AnonymousObjectMemberDeclaratorSyntax>(this.Locations);
            }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        public override bool IsIndexer
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return true; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return ImmutableArray<ParameterSymbol>.Empty; }
        }

        public override MethodSymbol SetMethod
        {
            get { return null; }
        }

        public override ImmutableArray<CustomModifier> TypeCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return Microsoft.Cci.CallingConvention.HasThis; }
        }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<PropertySymbol>.Empty; }
        }

        public override Symbol ContainingSymbol
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

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Public; }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return false; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        public override MethodSymbol GetMethod
        {
            get { return this.getMethod; }
        }

        public FieldSymbol BackingField
        {
            get { return this.backingField; }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return new LexicalSortKey(location, this.DeclaringCompilation);
        }

        internal override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken = default(CancellationToken))
        {
            return true;
        }
    }

    internal sealed class SynthesizedPropertyGetAccessorSymbol : SynthesizedInstanceMethodBaseForRecords
    {
        SynthesizedPropertySymbol property;

        internal SynthesizedPropertyGetAccessorSymbol(SynthesizedPropertySymbol property) : 
            base(containingType: property.ContainingType, 
                 name: SourcePropertyAccessorSymbol.GetAccessorName(property.Name, getNotSet: true, isWinMdOutput: false))
        {
            this.property = property;
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = CreateBoundNodeFactory(compilationState, diagnostics);
            F.CloseMethod(F.Block(F.Return(F.Field(F.This(), property.BackingField))));
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.PropertyGet; }
        }

        public override TypeSymbol ReturnType
        {
            get { return this.property.Type; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return this.property; }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                // The accessor for a anonymous type property has the same location as the property.
                return this.property.Locations;
            }
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            // Do not call base.AddSynthesizedAttributes.
            // Dev11 does not emit DebuggerHiddenAttribute in property accessors
        }

        internal override bool HasSpecialName
        {
            get { return true; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Public; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }
    }
}
