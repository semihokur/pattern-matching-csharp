// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class RecordParameterListSyntax
    {
        internal int ParameterCount
        {
            get
            {
                return this.Parameters.Count;
            }
        }
    }
}