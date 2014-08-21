// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitMatchStatement(BoundMatchStatement boundMatch)
        {
            var F = this.factory;
            var boolType = F.SpecialType(SpecialType.System_Boolean);

            var leftExpr = boundMatch.BoundExpression;

            var conditionsBuilder = ArrayBuilder<BoundExpression>.GetInstance();
            var blocksBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            bool isAnyDefaultLabel = false;
            foreach (var section in boundMatch.MatchSections)
            {
                BoundPattern boundPattern = null;
                BoundExpression current = null;

                foreach (var label in section.BoundMatchLabels)
                {
                    boundPattern = label.PatternOpt;

                    if (label.Syntax.Kind == SyntaxKind.DefaultSwitchLabel)
                    {
                        isAnyDefaultLabel = true;
                        break;
                    }
                    BoundExpression temp = new BoundMatchExpression(boundPattern.Syntax, leftExpr, boundPattern, boolType) { WasCompilerGenerated = true };

                    if (label.ConditionOpt != null)
                    {
                        temp = F.LogicalAnd(temp, label.ConditionOpt);
                    }

                    current = current == null ? (BoundExpression)temp : 
                                                F.Binary(BinaryOperatorKind.Or, F.SpecialType(SpecialType.System_Boolean), current, temp);
                }

                conditionsBuilder.Add((BoundExpression)Visit(current));

                // Break cannot be converted to the goto's because we cannot use VisitSwitchStatement(BoundSwitchStatement) method. This is BoundMatchStatement.
                // We just remove every statement after the break in the current section. If there is no break statement, we DO NOT add the statements from the next section.
                // TODO: We have to manually transform break statements to the goto statements accordingly. 
                var statements = section.Statements;
                bool isAnyBreakStatement = false;
                foreach (var statement in section.Statements)
                {
                    if (statement.Kind == BoundKind.BreakStatement)
                    {
                        isAnyBreakStatement = true;
                    }
                    if (isAnyBreakStatement)
                    {
                        statements = statements.Remove(statement);
                    }
                }
                blocksBuilder.Add((BoundBlock)Visit(F.Block(statements)));
            }

            BoundStatement previous;
            int length = blocksBuilder.Count;
            if (isAnyDefaultLabel)
            {
                previous = blocksBuilder[length - 1];
                length--;
            }
            else
            {
                previous = F.Block();
            }

            for (int i = length - 1; i >= 0; i--)
            {
                // We're creating nested if statements from the last section to the first one. 
                // We only add the locals in the if statement which is created for the first section. 
                // Because the if statements are created for the other sections are under the first one, they can still use these locals.
                previous = F.If(i == 0 ? boundMatch.InnerLocals : ImmutableArray<LocalSymbol>.Empty, conditionsBuilder[i], blocksBuilder[i], previous);
            }
            conditionsBuilder.Free();
            blocksBuilder.Free();
            return previous;
        }
    }
}
