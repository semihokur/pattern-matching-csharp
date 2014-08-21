// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents an interactive code entry point that is inserted into the compilation if there is not an existing one. 
    /// </summary>
    internal sealed class SynthesizedIsOperator : SynthesizedStaticMethodBaseForRecords
    {
        private readonly ImmutableArray<ParameterSymbol> parameters;
        private readonly TypeSymbol returnType;
        private readonly ImmutableArray<string> propertyNames;

        internal SynthesizedIsOperator(NamedTypeSymbol containingType, ImmutableArray<TypeSymbol> parameterTypes, ImmutableArray<string> parameterNames, ImmutableArray<string> propertyNames, NamedTypeSymbol returnType) :
            base(containingType, WellKnownMemberNames.IsOperatorName)
        {
            Debug.Assert((object)containingType != null);

            var paramBuilder = ArrayBuilder<ParameterSymbol>.GetInstance();

            paramBuilder.Add(new SynthesizedParameterSymbol(this, containingType, 0, RefKind.None, "o"));

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                paramBuilder.Add(new SynthesizedParameterSymbol(this, parameterTypes[i], i+1, RefKind.Out, parameterNames[i]));
            }

            this.parameters = paramBuilder.ToImmutableAndFree();
            this.returnType = returnType;
            this.propertyNames = propertyNames;
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = CreateBoundNodeFactory(compilationState, diagnostics);
            var statements = ArrayBuilder<BoundStatement>.GetInstance();
            var firstParameter = this.Parameters[0];

            for (int i = 1; i < this.Parameters.Length; i++)
            {
                var currentParam = this.Parameters[i];
                var property = GetPropertyForThisParameter(i - 1);

                var assignment = F.Assignment(F.Parameter(currentParam), F.Call(F.Parameter(firstParameter), property.GetMethod));
                statements.Add(assignment);
            }
            statements.Add(F.Return(F.Literal(true)));
            F.CloseMethod(F.Block(statements.ToImmutableAndFree()));
        }

        public PropertySymbol GetPropertyForThisParameter(int i)
        {
            var temp = this.ContainingType.GetMembers(propertyNames[i]);

            Debug.Assert(temp.Length == 1 && temp[0] is PropertySymbol);
            return (PropertySymbol) temp[0];
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return parameters; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Public; }
        }

        public override TypeSymbol ReturnType
        {
            get { return returnType; }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.BuiltinOperator; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }
    }
}
