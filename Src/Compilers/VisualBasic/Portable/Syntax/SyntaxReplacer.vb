﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Class SyntaxReplacer
        Friend Shared Function Replace(Of TNode As SyntaxNode)(
            root As SyntaxNode,
            Optional nodes As IEnumerable(Of TNode) = Nothing,
            Optional computeReplacementNode As Func(Of TNode, TNode, SyntaxNode) = Nothing,
            Optional tokens As IEnumerable(Of SyntaxToken) = Nothing,
            Optional computeReplacementToken As Func(Of SyntaxToken, SyntaxToken, SyntaxToken) = Nothing,
            Optional trivia As IEnumerable(Of SyntaxTrivia) = Nothing,
            Optional computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTrivia) = Nothing) As SyntaxNode

            Dim replacer = New Replacer(Of TNode)(nodes, computeReplacementNode, tokens, computeReplacementToken, trivia, computeReplacementTrivia)

            If replacer.HasWork Then
                Return replacer.Visit(root)
            Else
                Return root
            End If
        End Function

        Friend Shared Function Replace(
            root As SyntaxToken,
            Optional nodes As IEnumerable(Of SyntaxNode) = Nothing,
            Optional computeReplacementNode As Func(Of SyntaxNode, SyntaxNode, SyntaxNode) = Nothing,
            Optional tokens As IEnumerable(Of SyntaxToken) = Nothing,
            Optional computeReplacementToken As Func(Of SyntaxToken, SyntaxToken, SyntaxToken) = Nothing,
            Optional trivia As IEnumerable(Of SyntaxTrivia) = Nothing,
            Optional computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTrivia) = Nothing) As SyntaxToken

            Dim replacer = New Replacer(Of SyntaxNode)(nodes, computeReplacementNode, tokens, computeReplacementToken, trivia, computeReplacementTrivia)

            If replacer.HasWork Then
                Return replacer.VisitToken(root)
            Else
                Return root
            End If
        End Function

        Private Class Replacer(Of TNode As SyntaxNode)
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly computeReplacementNode As Func(Of TNode, TNode, SyntaxNode)
            Private ReadOnly computeReplacementToken As Func(Of SyntaxToken, SyntaxToken, SyntaxToken)
            Private ReadOnly computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTrivia)

            Private ReadOnly nodeSet As HashSet(Of SyntaxNode)
            Private ReadOnly tokenSet As HashSet(Of SyntaxToken)
            Private ReadOnly triviaSet As HashSet(Of SyntaxTrivia)

            Private ReadOnly spanSet As HashSet(Of TextSpan)
            Private ReadOnly totalSpan As TextSpan
            Private ReadOnly _visitStructuredTrivia As Boolean
            Private ReadOnly shouldVisitTrivia As Boolean

            Public Sub New(
                nodes As IEnumerable(Of TNode),
                computeReplacementNode As Func(Of TNode, TNode, SyntaxNode),
                tokens As IEnumerable(Of SyntaxToken),
                computeReplacementToken As Func(Of SyntaxToken, SyntaxToken, SyntaxToken),
                trivia As IEnumerable(Of SyntaxTrivia),
                computeReplacementTrivia As Func(Of SyntaxTrivia, SyntaxTrivia, SyntaxTrivia))

                Me.computeReplacementNode = computeReplacementNode
                Me.computeReplacementToken = computeReplacementToken
                Me.computeReplacementTrivia = computeReplacementTrivia

                Me.nodeSet = If(nodes IsNot Nothing, New HashSet(Of SyntaxNode)(nodes), NoNodes)
                Me.tokenSet = If(tokens IsNot Nothing, New HashSet(Of SyntaxToken)(tokens), NoTokens)
                Me.triviaSet = If(trivia IsNot Nothing, New HashSet(Of SyntaxTrivia)(trivia), NoTrivia)

                Me.spanSet = New HashSet(Of TextSpan)(Me.nodeSet.Select(Function(n) n.FullSpan).Concat(
                                                      Me.tokenSet.Select(Function(t) t.FullSpan)).Concat(
                                                      Me.triviaSet.Select(Function(t) t.FullSpan)))

                Me.totalSpan = ComputeTotalSpan(Me.spanSet)

                Me._visitStructuredTrivia = Me.nodeSet.Any(Function(n) n.IsPartOfStructuredTrivia()) OrElse
                    Me.tokenSet.Any(Function(t) t.IsPartOfStructuredTrivia()) OrElse
                    Me.triviaSet.Any(Function(t) t.IsPartOfStructuredTrivia())

                Me.shouldVisitTrivia = Me.triviaSet.Count > 0 OrElse Me._visitStructuredTrivia
            End Sub

            Private Shared ReadOnly NoNodes As HashSet(Of SyntaxNode) = New HashSet(Of SyntaxNode)()
            Private Shared ReadOnly NoTokens As HashSet(Of SyntaxToken) = New HashSet(Of SyntaxToken)()
            Private Shared ReadOnly NoTrivia As HashSet(Of SyntaxTrivia) = New HashSet(Of SyntaxTrivia)()

            Public Overrides ReadOnly Property VisitIntoStructuredTrivia As Boolean
                Get
                    Return Me._visitStructuredTrivia
                End Get
            End Property

            Public ReadOnly Property HasWork As Boolean
                Get
                    Return Me.nodeSet.Count + Me.tokenSet.Count + Me.triviaSet.Count > 0
                End Get
            End Property

            Private Shared Function ComputeTotalSpan(spans As IEnumerable(Of TextSpan)) As TextSpan
                Dim first = True
                Dim start As Integer = 0
                Dim [end] As Integer = 0

                For Each span In spans
                    If first Then
                        first = False
                        start = span.Start
                        [end] = span.End
                    Else
                        start = Math.Min(start, span.Start)
                        [end] = Math.Max([end], span.End)
                    End If
                Next

                Return New TextSpan(start, [end] - start)
            End Function

            Private Function ShouldVisit(span As TextSpan) As Boolean
                If Not span.IntersectsWith(Me.totalSpan) Then
                    Return False
                End If

                For Each s In Me.spanSet
                    If span.IntersectsWith(s) Then
                        Return True
                    End If
                Next

                Return False
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                Dim rewritten = node
                If node IsNot Nothing Then
                    If Me.ShouldVisit(node.FullSpan) Then
                        rewritten = MyBase.Visit(node)
                    End If

                    If Me.nodeSet.Contains(node) AndAlso Me.computeReplacementNode IsNot Nothing Then
                        rewritten = Me.computeReplacementNode(DirectCast(node, TNode), DirectCast(rewritten, TNode))
                    End If
                End If

                Return rewritten
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim rewritten = token

                If Me.shouldVisitTrivia AndAlso Me.ShouldVisit(token.FullSpan) Then
                    rewritten = MyBase.VisitToken(token)
                End If

                If Me.tokenSet.Contains(token) AndAlso Me.computeReplacementToken IsNot Nothing Then
                    rewritten = Me.computeReplacementToken(token, rewritten)
                End If

                Return rewritten
            End Function

            Public Overrides Function VisitListElement(trivia As SyntaxTrivia) As SyntaxTrivia
                Dim rewritten = trivia

                If Me.VisitIntoStructuredTrivia AndAlso trivia.HasStructure AndAlso Me.ShouldVisit(trivia.FullSpan) Then
                    rewritten = Me.VisitTrivia(trivia)
                End If

                If Me.triviaSet.Contains(trivia) AndAlso Me.computeReplacementTrivia IsNot Nothing Then
                    rewritten = Me.computeReplacementTrivia(trivia, rewritten)
                End If

                Return rewritten
            End Function
        End Class

        Public Shared Function ReplaceNodeInList(root As SyntaxNode, originalNode As SyntaxNode, newNodes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Return New NodeListEditor(originalNode, newNodes, ListEditKind.Replace).Visit(root)
        End Function

        Public Shared Function InsertNodeInList(root As SyntaxNode, nodeInList As SyntaxNode, nodesToInsert As IEnumerable(Of SyntaxNode), insertBefore As Boolean) As SyntaxNode
            Return New NodeListEditor(nodeInList, nodesToInsert, If(insertBefore, ListEditKind.InsertBefore, ListEditKind.InsertAfter)).Visit(root)
        End Function

        Public Shared Function ReplaceTokenInList(root As SyntaxNode, tokenInList As SyntaxToken, newTokens As IEnumerable(Of SyntaxToken)) As SyntaxNode
            Return New TokenListEditor(tokenInList, newTokens, ListEditKind.Replace).Visit(root)
        End Function

        Public Shared Function InsertTokenInList(root As SyntaxNode, tokenInList As SyntaxToken, newTokens As IEnumerable(Of SyntaxToken), insertBefore As Boolean) As SyntaxNode
            Return New TokenListEditor(tokenInList, newTokens, If(insertBefore, ListEditKind.InsertBefore, ListEditKind.InsertAfter)).Visit(root)
        End Function

        Public Shared Function ReplaceTriviaInList(root As SyntaxNode, triviaInList As SyntaxTrivia, newTrivia As IEnumerable(Of SyntaxTrivia)) As SyntaxNode
            Return New TriviaListEditor(triviaInList, newTrivia, ListEditKind.Replace).Visit(root)
        End Function

        Public Shared Function InsertTriviaInList(root As SyntaxNode, triviaInList As SyntaxTrivia, newTrivia As IEnumerable(Of SyntaxTrivia), insertBefore As Boolean) As SyntaxNode
            Return New TriviaListEditor(triviaInList, newTrivia, If(insertBefore, ListEditKind.InsertBefore, ListEditKind.InsertAfter)).Visit(root)
        End Function

        Public Shared Function ReplaceTriviaInList(root As SyntaxToken, triviaInList As SyntaxTrivia, newTrivia As IEnumerable(Of SyntaxTrivia)) As SyntaxToken
            Return New TriviaListEditor(triviaInList, newTrivia, ListEditKind.Replace).VisitToken(root)
        End Function

        Public Shared Function InsertTriviaInList(root As SyntaxToken, triviaInList As SyntaxTrivia, newTrivia As IEnumerable(Of SyntaxTrivia), insertBefore As Boolean) As SyntaxToken
            Return New TriviaListEditor(triviaInList, newTrivia, If(insertBefore, ListEditKind.InsertBefore, ListEditKind.InsertAfter)).VisitToken(root)
        End Function

        Private Enum ListEditKind
            InsertBefore
            InsertAfter
            Replace
        End Enum

        Private Shared Function GetItemNotListElementException() As InvalidOperationException
            Return New InvalidOperationException("The item specified is not the element of a list.".NeedsLocalization())
        End Function

        Private Class BaseListEditor
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _elementSpan As TextSpan
            Protected ReadOnly _editKind As ListEditKind
            Private ReadOnly _visitTrivia As Boolean
            Private ReadOnly _visitIntoStructuredTrivia As Boolean

            Public Sub New(
                elementSpan As TextSpan,
                editKind As ListEditKind,
                visitTrivia As Boolean,
                visitIntoStructuredTrivia As Boolean)
                Me._elementSpan = elementSpan
                Me._editKind = editKind
                Me._visitTrivia = visitTrivia Or visitIntoStructuredTrivia
                Me._visitIntoStructuredTrivia = visitIntoStructuredTrivia
            End Sub

            Public Overrides ReadOnly Property VisitIntoStructuredTrivia As Boolean
                Get
                    Return Me._visitIntoStructuredTrivia
                End Get
            End Property

            Private Function ShouldVisit(span As TextSpan) As Boolean
                Return span.IntersectsWith(Me._elementSpan)
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                Dim rewritten = node
                If node IsNot Nothing AndAlso ShouldVisit(node.FullSpan) Then
                    rewritten = MyBase.Visit(node)
                End If
                Return rewritten
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim rewritten = token
                If Me._visitTrivia AndAlso Me.ShouldVisit(token.FullSpan) Then
                    rewritten = MyBase.VisitToken(token)
                End If
                Return rewritten
            End Function

            Public Overrides Function VisitListElement(element As SyntaxTrivia) As SyntaxTrivia
                Dim rewritten = element
                If Me._visitIntoStructuredTrivia AndAlso element.HasStructure AndAlso Me.ShouldVisit(element.FullSpan) Then
                    rewritten = MyBase.VisitTrivia(element)
                End If
                Return rewritten
            End Function
        End Class

        Private Class NodeListEditor
            Inherits BaseListEditor

            Private ReadOnly _originalNode As SyntaxNode
            Private ReadOnly _replacementNodes As IEnumerable(Of SyntaxNode)

            Public Sub New(originalNode As SyntaxNode, replacementNodes As IEnumerable(Of SyntaxNode), editKind As ListEditKind)
                MyBase.New(originalNode.FullSpan, editKind, visitTrivia:=False, visitIntoStructuredTrivia:=originalNode.IsPartOfStructuredTrivia())
                Me._originalNode = originalNode
                Me._replacementNodes = replacementNodes
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If node Is _originalNode Then
                    Throw GetItemNotListElementException()
                End If

                Return MyBase.Visit(node)
            End Function

            Public Overrides Function VisitList(Of TNode As SyntaxNode)(list As SeparatedSyntaxList(Of TNode)) As SeparatedSyntaxList(Of TNode)
                If TypeOf Me._originalNode Is TNode Then
                    Dim index = list.IndexOf(DirectCast(Me._originalNode, TNode))
                    If index >= 0 AndAlso index < list.Count Then
                        Select Case Me._editKind
                            Case ListEditKind.Replace
                                Return list.ReplaceRange(DirectCast(Me._originalNode, TNode), Me._replacementNodes.Cast(Of TNode))
                            Case ListEditKind.InsertBefore
                                Return list.InsertRange(index, Me._replacementNodes.Cast(Of TNode))
                            Case ListEditKind.InsertAfter
                                Return list.InsertRange(index + 1, Me._replacementNodes.Cast(Of TNode))
                        End Select
                    End If
                End If
                Return MyBase.VisitList(list)
            End Function

            Public Overrides Function VisitList(Of TNode As SyntaxNode)(list As SyntaxList(Of TNode)) As SyntaxList(Of TNode)
                If TypeOf Me._originalNode Is TNode Then
                    Dim index = list.IndexOf(DirectCast(Me._originalNode, TNode))
                    If index >= 0 AndAlso index < list.Count Then
                        Select Case Me._editKind
                            Case ListEditKind.Replace
                                Return list.ReplaceRange(DirectCast(Me._originalNode, TNode), Me._replacementNodes.Cast(Of TNode))
                            Case ListEditKind.InsertBefore
                                Return list.InsertRange(index, Me._replacementNodes.Cast(Of TNode))
                            Case ListEditKind.InsertAfter
                                Return list.InsertRange(index + 1, Me._replacementNodes.Cast(Of TNode))
                        End Select
                    End If
                End If
                Return MyBase.VisitList(list)
            End Function
        End Class

        Private Class TokenListEditor
            Inherits BaseListEditor

            Private ReadOnly _originalToken As SyntaxToken
            Private ReadOnly _newTokens As IEnumerable(Of SyntaxToken)

            Public Sub New(originalToken As SyntaxToken, newTokens As IEnumerable(Of SyntaxToken), editKind As ListEditKind)
                MyBase.New(originalToken.FullSpan, editKind, visitTrivia:=False, visitIntoStructuredTrivia:=originalToken.IsPartOfStructuredTrivia())
                Me._originalToken = originalToken
                Me._newTokens = newTokens
            End Sub

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                If token = _originalToken Then
                    Throw GetItemNotListElementException()
                End If

                Return MyBase.VisitToken(token)
            End Function

            Public Overrides Function VisitList(list As SyntaxTokenList) As SyntaxTokenList
                Dim index = list.IndexOf(Me._originalToken)
                If index >= 0 AndAlso index < list.Count Then
                    Select Case Me._editKind
                        Case ListEditKind.Replace
                            Return list.ReplaceRange(Me._originalToken, Me._newTokens)
                        Case ListEditKind.InsertBefore
                            Return list.InsertRange(index, Me._newTokens)
                        Case ListEditKind.InsertAfter
                            Return list.InsertRange(index + 1, Me._newTokens)
                    End Select
                End If
                Return MyBase.VisitList(list)
            End Function
        End Class

        Private Class TriviaListEditor
            Inherits BaseListEditor

            Private ReadOnly _originalTrivia As SyntaxTrivia
            Private ReadOnly _newTrivia As IEnumerable(Of SyntaxTrivia)

            Public Sub New(originalTrivia As SyntaxTrivia, newTrivia As IEnumerable(Of SyntaxTrivia), editKind As ListEditKind)
                MyBase.New(originalTrivia.FullSpan, editKind, visitTrivia:=True, visitIntoStructuredTrivia:=originalTrivia.IsPartOfStructuredTrivia())
                Me._originalTrivia = originalTrivia
                Me._newTrivia = newTrivia
            End Sub

            Public Overrides Function VisitList(list As SyntaxTriviaList) As SyntaxTriviaList
                Dim index = list.IndexOf(Me._originalTrivia)
                If index >= 0 AndAlso index < list.Count Then
                    Select Case Me._editKind
                        Case ListEditKind.Replace
                            Return list.ReplaceRange(Me._originalTrivia, Me._newTrivia)
                        Case ListEditKind.InsertBefore
                            Return list.InsertRange(index, Me._newTrivia)
                        Case ListEditKind.InsertAfter
                            Return list.InsertRange(index + 1, Me._newTrivia)
                    End Select
                End If
                Return MyBase.VisitList(list)
            End Function
        End Class

    End Class
End Namespace
