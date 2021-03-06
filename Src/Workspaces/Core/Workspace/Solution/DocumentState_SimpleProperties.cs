﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState
    {
        public HostLanguageServices LanguageServices
        {
            get { return this.languageServices; }
        }

        public ParseOptions ParseOptions
        {
            get { return this.options; }
        }

        public SourceCodeKind SourceCodeKind
        {
            get
            {
                return this.ParseOptions == null ? SourceCodeKind.Regular : this.ParseOptions.Kind;
            }
        }

        public bool IsGenerated
        {
            get { return this.info.IsGenerated; }
        }
    }
}