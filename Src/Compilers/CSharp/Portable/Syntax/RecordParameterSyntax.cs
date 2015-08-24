// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal interface IParameterSyntax
    {
        EqualsValueClauseSyntax Default { get; }
        SyntaxList<AttributeListSyntax> AttributeLists { get; }
        SyntaxReference GetReference();
    }

    public partial class RecordParameterSyntax : IParameterSyntax
    {
        internal bool IsArgList
        {
            get
            {
                return this.Type == null && this.Identifier.CSharpContextualKind() == SyntaxKind.ArgListKeyword;
            }
        }
    }

    public partial class ParameterSyntax : IParameterSyntax
    {
    }
}