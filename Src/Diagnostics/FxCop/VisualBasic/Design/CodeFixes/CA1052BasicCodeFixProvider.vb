﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.FxCopAnalyzers
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design

    <ExportCodeFixProvider(StaticTypeRulesDiagnosticAnalyzer.RuleNameForExportAttribute, LanguageNames.VisualBasic)>
    Public Class CA1052BasicCodeFixProvider
        Implements ICodeFixProvider

        Public Function GetFixableDiagnosticIds() As IEnumerable(Of String) Implements ICodeFixProvider.GetFixableDiagnosticIds
            Return {StaticTypeRulesDiagnosticAnalyzer.CA1052RuleId}
        End Function

        Public Async Function GetFixesAsync(document As Document, span As TextSpan, diagnostics As IEnumerable(Of Diagnostic), cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CodeAction)) Implements ICodeFixProvider.GetFixesAsync
            cancellationToken.ThrowIfCancellationRequested()
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken)
            Dim classStatement = root.FindToken(span.Start).GetAncestor(Of ClassStatementSyntax)
            If classStatement IsNot Nothing Then
                Dim notInheritableKeyword = SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword).WithAdditionalAnnotations(Formatter.Annotation)
                Dim newClassStatement = classStatement.AddModifiers(notInheritableKeyword)
                Dim newRoot = root.ReplaceNode(classStatement, newClassStatement)
                Return {CodeAction.Create(String.Format(FxCopRulesResources.StaticHolderTypeIsNotStatic, classStatement.Identifier.Text), document.WithSyntaxRoot(newRoot))}
            End If

            Return Nothing
        End Function

    End Class
End Namespace
