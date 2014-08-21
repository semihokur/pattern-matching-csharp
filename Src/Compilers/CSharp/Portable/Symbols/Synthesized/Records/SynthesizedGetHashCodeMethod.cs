// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedGetHashCodeMethod : SynthesizedInstanceMethodBaseForRecords
    {
        private readonly TypeSymbol returnType;

        internal SynthesizedGetHashCodeMethod(NamedTypeSymbol container, NamedTypeSymbol returnType)
            : base(container, WellKnownMemberNames.ObjectGetHashCode)
        {
            this.returnType = returnType;
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = this.CreateBoundNodeFactory(compilationState, diagnostics);

            //  Method body:
            //
            //  HASH_FACTOR = 0xa5555529;
            //  INIT_HASH = (...((0 * HASH_FACTOR) + backingFld_1.Name.GetHashCode()) * HASH_FACTOR
            //                                     + backingFld_2.Name.GetHashCode()) * HASH_FACTOR
            //                                     + ...
            //                                     + backingFld_N.Name.GetHashCode()
            //
            //  {
            //      return (...((INITIAL_HASH * HASH_FACTOR) + EqualityComparer<T_1>.Default.GetHashCode(this.backingFld_1)) * HASH_FACTOR
            //                                               + EqualityComparer<T_2>.Default.GetHashCode(this.backingFld_2)) * HASH_FACTOR
            //                                               ...
            //                                               + EqualityComparer<T_N>.Default.GetHashCode(this.backingFld_N)
            //  }

            const int HASH_FACTOR = unchecked((int)0xa5555529); // (int)0xa5555529

            // Type expression
            var container = this.ContainingType;

            //  INIT_HASH
            int initHash = 0;

            foreach (var m in container.GetMembers())
            {
                if (m is SynthesizedPropertySymbol)
                {
                    var property = (SynthesizedPropertySymbol)m;
                    initHash = unchecked(initHash * HASH_FACTOR + property.BackingField.Name.GetHashCode());
                }
            }

            //  Generate expression for return statement
            //      retExpression <= 'INITIAL_HASH'
            BoundExpression retExpression = F.Literal(initHash);

            //  prepare symbols
            MethodSymbol equalityComparer_GetHashCode = F.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode) as MethodSymbol;
            MethodSymbol equalityComparer_get_Default = F.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default) as MethodSymbol;
            NamedTypeSymbol equalityComparerType = equalityComparer_GetHashCode.ContainingType;

            //  bound HASH_FACTOR
            BoundLiteral boundHashFactor = F.Literal(HASH_FACTOR);

            // Process fields
            foreach (var m in container.GetMembers())
            {
                var property = m as SynthesizedPropertySymbol;
                if (!ReferenceEquals(property,null))
                {
                    var backingField = property.BackingField;

                    NamedTypeSymbol constructedEqualityComparer = equalityComparerType.Construct(backingField.Type);

                    // Generate 'retExpression' <= 'retExpression * HASH_FACTOR 
                    retExpression = F.Binary(BinaryOperatorKind.IntMultiplication, F.SpecialType(SpecialType.System_Int32), retExpression, boundHashFactor);

                    // Generate 'retExpression' <= 'retExpression + EqualityComparer<T_index>.Default.GetHashCode(this.backingFld_index)'
                    retExpression = F.Binary(BinaryOperatorKind.IntAddition,
                                             F.SpecialType(SpecialType.System_Int32),
                                             retExpression,
                                             F.Call(
                                                F.StaticCall(constructedEqualityComparer,
                                                             equalityComparer_get_Default.AsMember(constructedEqualityComparer)),
                                                equalityComparer_GetHashCode.AsMember(constructedEqualityComparer),
                                                F.Field(F.This(), backingField)));
                }
            }

            // Create a bound block 
            F.CloseMethod(F.Block(F.Return(retExpression)));
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.Ordinary; }
        }


        public override TypeSymbol ReturnType
        {
            get { return this.returnType; }
        }

        public override bool IsOverride
        {
            get { return true; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Public; }
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return true;
        }
    }
}
