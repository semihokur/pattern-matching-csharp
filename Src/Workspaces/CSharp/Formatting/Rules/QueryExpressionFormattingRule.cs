﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
#if MEF
    [ExportFormattingRule(Name, LanguageNames.CSharp)]
    [ExtensionOrder(After = AnchorIndentationFormattingRule.Name)]
#endif
    internal class QueryExpressionFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Query Expressions Formatting Rule";

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<SuppressOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            var queryExpression = node as QueryExpressionSyntax;
            if (queryExpression != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, queryExpression.GetFirstToken(includeZeroWidth: true), queryExpression.GetLastToken(includeZeroWidth: true));
            }
        }

        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            var queryExpression = node as QueryExpressionSyntax;
            if (queryExpression != null)
            {
                var firstToken = queryExpression.FromClause.Expression.GetFirstToken(includeZeroWidth: true);
                var lastToken = queryExpression.FromClause.Expression.GetLastToken(includeZeroWidth: true);
                AddIndentBlockOperation(list, queryExpression.FromClause.FromKeyword, firstToken, lastToken);

                for (int i = 0; i < queryExpression.Body.Clauses.Count; i++)
                {
                    // if it is nested query expression
                    var fromClause = queryExpression.Body.Clauses[i] as FromClauseSyntax;
                    if (fromClause != null)
                    {
                        firstToken = fromClause.Expression.GetFirstToken(includeZeroWidth: true);
                        lastToken = fromClause.Expression.GetLastToken(includeZeroWidth: true);
                        AddIndentBlockOperation(list, fromClause.FromKeyword, firstToken, lastToken);
                    }
                }

                // set alignment line for query expression
                var baseToken = queryExpression.GetFirstToken(includeZeroWidth: true);
                var endToken = queryExpression.GetLastToken(includeZeroWidth: true);
                if (!baseToken.IsMissing && !baseToken.Equals(endToken))
                {
                    var startToken = baseToken.GetNextToken(includeZeroWidth: true);
                    SetAlignmentBlockOperation(list, baseToken, startToken, endToken);
                }
            }
        }

        public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AnchorIndentationOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            var queryClause = node as QueryClauseSyntax;
            if (queryClause != null)
            {
                var firstToken = queryClause.GetFirstToken(includeZeroWidth: true);
                AddAnchorIndentationOperation(list, firstToken, queryClause.GetLastToken(includeZeroWidth: true));
            }

            var selectOrGroupClause = node as SelectOrGroupClauseSyntax;
            if (selectOrGroupClause != null)
            {
                var firstToken = selectOrGroupClause.GetFirstToken(includeZeroWidth: true);
                AddAnchorIndentationOperation(list, firstToken, selectOrGroupClause.GetLastToken(includeZeroWidth: true));
            }

            var continuation = node as QueryContinuationSyntax;
            if (continuation != null)
            {
                AddAnchorIndentationOperation(list, continuation.IntoKeyword, continuation.GetLastToken(includeZeroWidth: true));
            }
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            if (previousToken.IsNestedQueryExpression())
            {
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            // skip the very first from keyword
            if (currentToken.IsFirstFromKeywordInExpression())
            {
                return nextOperation.Invoke();
            }

            switch (currentToken.CSharpKind())
            {
                case SyntaxKind.FromKeyword:
                case SyntaxKind.WhereKeyword:
                case SyntaxKind.LetKeyword:
                case SyntaxKind.JoinKeyword:
                case SyntaxKind.OrderByKeyword:
                case SyntaxKind.GroupKeyword:
                case SyntaxKind.SelectKeyword:
                    if (currentToken.GetAncestor<QueryExpressionSyntax>() != null)
                    {
                        if (optionSet.GetOption(CSharpFormattingOptions.NewLineForClausesInQuery))
                        {
                            return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                        }
                        else
                        {
                            return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
                        }
                    }

                    break;
            }

            return nextOperation.Invoke();
        }
    }
}
