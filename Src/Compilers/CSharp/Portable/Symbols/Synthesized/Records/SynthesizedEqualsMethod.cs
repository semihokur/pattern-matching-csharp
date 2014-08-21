// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedEqualsMethod : SynthesizedInstanceMethodBaseForRecords
    {
        private readonly TypeSymbol returnType;
        private readonly ImmutableArray<ParameterSymbol> parameters;

        internal SynthesizedEqualsMethod(NamedTypeSymbol container, NamedTypeSymbol returnBoolType, NamedTypeSymbol paramObjectType)
            : base(container, WellKnownMemberNames.ObjectEquals)
        {
            this.returnType = returnBoolType;
            this.parameters = ImmutableArray.Create<ParameterSymbol>(
                      new SynthesizedParameterSymbol(this, paramObjectType, 0, RefKind.None, "value")
                  );
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = this.CreateBoundNodeFactory(compilationState, diagnostics);

            //  Method body:
            //
            //  {
            //      $anonymous$ local = value as $anonymous$;
            //      return local != null 
            //             && System.Collections.Generic.EqualityComparer<T_1>.Default.Equals(this.backingFld_1, local.backingFld_1)
            //             ...
            //             && System.Collections.Generic.EqualityComparer<T_N>.Default.Equals(this.backingFld_N, local.backingFld_N);
            //  }

            // Type and type expression
            NamedTypeSymbol container = this.ContainingType;

            //  local
            BoundAssignmentOperator assignmentToTemp;
            BoundLocal boundLocal = F.StoreToTemp(F.As(F.Parameter(this.parameters[0]), container), out assignmentToTemp);

            //  Generate: statement <= 'local = value as $anonymous$'
            BoundStatement assignment = F.ExpressionStatement(assignmentToTemp);

            //  Generate expression for return statement
            //      retExpression <= 'local != null'
            BoundExpression retExpression = F.Binary(BinaryOperatorKind.ObjectNotEqual,
                                                     F.SpecialType(SpecialType.System_Boolean),
                                                     F.Convert(F.SpecialType(SpecialType.System_Object), boundLocal),
                                                     F.Null(F.SpecialType(SpecialType.System_Object)));

            //  prepare symbols
            MethodSymbol equalityComparer_Equals = F.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__Equals) as MethodSymbol;
            MethodSymbol equalityComparer_get_Default = F.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default) as MethodSymbol;
            NamedTypeSymbol equalityComparerType = equalityComparer_Equals.ContainingType;

            foreach (var m in container.GetMembers())
            {
                var property = m as SynthesizedPropertySymbol;
                if (!ReferenceEquals(property,null))
                {
                    var backingField = property.BackingField;

                    NamedTypeSymbol constructedEqualityComparer = equalityComparerType.Construct(backingField.Type);

                    // Generate 'retExpression' = 'retExpression && System.Collections.Generic.EqualityComparer<T_index>.
                    //                                                  Default.Equals(this.backingFld_index, local.backingFld_index)'
                    retExpression = F.LogicalAnd(retExpression,
                                                 F.Call(F.StaticCall(constructedEqualityComparer,
                                                                     equalityComparer_get_Default.AsMember(constructedEqualityComparer)),
                                                        equalityComparer_Equals.AsMember(constructedEqualityComparer),
                                                        F.Field(F.This(), backingField),
                                                        F.Call(boundLocal, property.GetMethod)));
                }
            }
            // Final return statement
            BoundStatement retStatement = F.Return(retExpression);

            // Create a bound block 
            F.CloseMethod(F.Block(ImmutableArray.Create<LocalSymbol>(boundLocal.LocalSymbol), assignment, retStatement));
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return parameters; }
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
