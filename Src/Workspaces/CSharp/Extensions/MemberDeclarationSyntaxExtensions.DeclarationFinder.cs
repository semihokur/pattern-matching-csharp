﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class MemberDeclarationSyntaxExtensions
    {
        private sealed class DeclarationFinder : CSharpSyntaxWalker
        {
            private readonly Dictionary<string, List<SyntaxToken>> map = new Dictionary<string, List<SyntaxToken>>();

            private DeclarationFinder()
                : base(SyntaxWalkerDepth.Node)
            {
            }

            public static Dictionary<string, List<SyntaxToken>> GetAllDeclarations(SyntaxNode syntax)
            {
                var finder = new DeclarationFinder();
                finder.Visit(syntax);
                return finder.map;
            }

            private void Add(SyntaxToken syntaxToken)
            {
                if (syntaxToken.CSharpKind() == SyntaxKind.IdentifierToken)
                {
                    var identifier = syntaxToken.ValueText;
                    List<SyntaxToken> list;
                    if (!map.TryGetValue(identifier, out list))
                    {
                        list = new List<SyntaxToken>();
                        map.Add(identifier, list);
                    }

                    list.Add(syntaxToken);
                }
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                base.VisitVariableDeclarator(node);
                Add(node.Identifier);
            }

            public override void VisitCatchDeclaration(CatchDeclarationSyntax node)
            {
                base.VisitCatchDeclaration(node);
                Add(node.Identifier);
            }

            public override void VisitParameter(ParameterSyntax node)
            {
                base.VisitParameter(node);
                Add(node.Identifier);
            }

            public override void VisitFromClause(FromClauseSyntax node)
            {
                base.VisitFromClause(node);
                Add(node.Identifier);
            }

            public override void VisitLetClause(LetClauseSyntax node)
            {
                base.VisitLetClause(node);
                Add(node.Identifier);
            }

            public override void VisitJoinClause(JoinClauseSyntax node)
            {
                base.VisitJoinClause(node);
                Add(node.Identifier);
            }

            public override void VisitJoinIntoClause(JoinIntoClauseSyntax node)
            {
                base.VisitJoinIntoClause(node);
                Add(node.Identifier);
            }

            public override void VisitQueryContinuation(QueryContinuationSyntax node)
            {
                base.VisitQueryContinuation(node);
                Add(node.Identifier);
            }
        }
    }
}