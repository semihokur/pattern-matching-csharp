﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The visitor which searches for a bound node inside a bound subtree
    ''' </summary>
    Friend Class BoundNodeFinder
        Inherits BoundTreeWalker

        Public Shared Function ContainsNode(findWhere As BoundNode, findWhat As BoundNode) As Boolean
            Debug.Assert(findWhere IsNot Nothing)
            Debug.Assert(findWhat IsNot Nothing)

            If findWhere Is findWhat Then
                Return True
            End If

            Dim walker As New BoundNodeFinder(findWhat)
            walker.Visit(findWhere)
            Return walker._nodeToFind Is Nothing
        End Function

        Private Sub New(_nodeToFind As BoundNode)
            Me._nodeToFind = _nodeToFind
        End Sub

        ''' <summary> Note: Nothing if node is found </summary>
        Private _nodeToFind As BoundNode

        Public Overrides Function Visit(node As BoundNode) As BoundNode
            If Me._nodeToFind IsNot Nothing Then
                If Me._nodeToFind Is node Then
                    Me._nodeToFind = Nothing
                Else
                    MyBase.Visit(node)
                End If
            End If
            Return Nothing
        End Function

        Public Overrides Function VisitUnboundLambda(node As UnboundLambda) As BoundNode
            Visit(node.BindForErrorRecovery())
            Return Nothing
        End Function
    End Class

End Namespace