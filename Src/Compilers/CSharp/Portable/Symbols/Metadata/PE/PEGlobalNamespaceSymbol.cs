﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal sealed class PEGlobalNamespaceSymbol
        : PENamespaceSymbol
    {
        /// <summary>
        /// The module containing the namespace.
        /// </summary>
        /// <remarks></remarks>
        private readonly PEModuleSymbol moduleSymbol;

        internal PEGlobalNamespaceSymbol(PEModuleSymbol moduleSymbol)
        {
            Debug.Assert((object)moduleSymbol != null);
            this.moduleSymbol = moduleSymbol;
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return moduleSymbol;
            }
        }

        internal override PEModuleSymbol ContainingPEModule
        {
            get
            {
                return moduleSymbol;
            }
        }

        public override string Name
        {
            get
            {
                return string.Empty;
            }
        }

        public override bool IsGlobalNamespace
        {
            get
            {
                return true;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return moduleSymbol.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return moduleSymbol;
            }
        }

        protected override void EnsureAllMembersLoaded()
        {
            if (lazyTypes == null || lazyNamespaces == null)
            {
                IEnumerable<IGrouping<string, TypeHandle>> groups;

                try
                {
                    groups = moduleSymbol.Module.GroupTypesByNamespaceOrThrow(System.StringComparer.Ordinal);
                }
                catch (BadImageFormatException)
                {
                    groups = SpecializedCollections.EmptyEnumerable<IGrouping<string, TypeHandle>>();
                }

                LoadAllMembers(groups);
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}