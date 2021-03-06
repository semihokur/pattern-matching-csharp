﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationTypeParameterSymbol : CodeGenerationTypeSymbol, ITypeParameterSymbol
    {
        public VarianceKind Variance { get; private set; }
        public ImmutableArray<ITypeSymbol> ConstraintTypes { get; internal set; }
        public bool HasConstructorConstraint { get; private set; }
        public bool HasReferenceTypeConstraint { get; private set; }
        public bool HasValueTypeConstraint { get; private set; }
        public int Ordinal { get; private set; }

        public CodeGenerationTypeParameterSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            VarianceKind varianceKind,
            string name,
            ImmutableArray<ITypeSymbol> constraintTypes,
            bool hasConstructorConstraint,
            bool hasReferenceConstraint,
            bool hasValueConstraint,
            int ordinal)
            : base(containingType, attributes, Accessibility.NotApplicable, default(SymbolModifiers), name, SpecialType.None)
        {
            this.Variance = varianceKind;
            this.ConstraintTypes = constraintTypes;
            this.Ordinal = ordinal;
            this.HasConstructorConstraint = hasConstructorConstraint;
            this.HasReferenceTypeConstraint = hasReferenceConstraint;
            this.HasValueTypeConstraint = hasValueConstraint;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationTypeParameterSymbol(
                this.ContainingType, this.GetAttributes(), this.Variance, this.Name,
                this.ConstraintTypes, this.HasConstructorConstraint, this.HasReferenceTypeConstraint,
                this.HasValueTypeConstraint, this.Ordinal);
        }

        public new ITypeParameterSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        public ITypeParameterSymbol ReducedFrom
        {
            get
            {
                return null;
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.TypeParameter;
            }
        }

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitTypeParameter(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitTypeParameter(this);
        }

        public override TypeKind TypeKind
        {
            get
            {
                return TypeKind.TypeParameter;
            }
        }

        public TypeParameterKind TypeParameterKind
        {
            get
            {
                return this.DeclaringMethod != null
                    ? TypeParameterKind.Method
                    : TypeParameterKind.Type;
            }
        }

        public IMethodSymbol DeclaringMethod
        {
            get
            {
                return this.ContainingSymbol as IMethodSymbol;
            }
        }

        public INamedTypeSymbol DeclaringType
        {
            get
            {
                return this.ContainingSymbol as INamedTypeSymbol;
            }
        }
    }
}