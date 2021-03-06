﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A field of a frame class that represents a variable that has been captured in a lambda.
    ''' </summary>
    Friend NotInheritable Class LambdaCapturedVariable
        Inherits SynthesizedFieldSymbol

        Private ReadOnly m_isMe As Boolean

        Friend Sub New(frame As LambdaFrame, captured As Symbol, type As TypeSymbol, fieldName As String, isMeParameter As Boolean)
            MyBase.New(frame, captured, type, fieldName, accessibility:=Accessibility.Public)

            Me.m_isMe = isMeParameter
        End Sub

        Public Shared Function Create(frame As LambdaFrame, captured As Symbol) As LambdaCapturedVariable
            Debug.Assert(TypeOf captured Is LocalSymbol OrElse TypeOf captured Is ParameterSymbol)

            Dim fieldName = GetCapturedVariableFieldName(captured)
            Dim type = GetCapturedVariableFieldType(frame, captured)

            Return New LambdaCapturedVariable(frame, captured, type, fieldName, IsMe(captured))
        End Function

        Public Shared Function GetCapturedVariableFieldName(captured As Symbol) As String
            Dim name As String

            ' captured symbol is either a local or parameter.
            Dim local = TryCast(captured, LocalSymbol)

            If local IsNot Nothing Then
                ' it is a local variable
                name = If(local.IsCompilerGenerated,
                          StringConstants.LiftedNonLocalPrefix & captured.Name,
                          StringConstants.LiftedLocalPrefix & captured.Name)
            Else
                ' it must be a parameter
                Dim parameter = DirectCast(captured, ParameterSymbol)
                name = If(parameter.IsMe,
                          StringConstants.LiftedMeName,
                          StringConstants.LiftedLocalPrefix & captured.Name)
            End If

            Return name
        End Function

        Public Shared Function GetCapturedVariableFieldType(frame As LambdaFrame, captured As Symbol) As TypeSymbol
            Dim type As TypeSymbol

            ' captured symbol is either a local or parameter.
            Dim local = TryCast(captured, LocalSymbol)

            If local IsNot Nothing Then
                ' it is a local variable
                Dim localTypeAsFrame = TryCast(local.Type.OriginalDefinition, LambdaFrame)
                If localTypeAsFrame IsNot Nothing Then
                    ' if we're capturing a generic frame pointer, construct it with the new frame's type parameters
                    type = LambdaRewriter.ConstructFrameType(localTypeAsFrame, frame.TypeArgumentsNoUseSiteDiagnostics)
                Else
                    type = local.Type.InternalSubstituteTypeParameters(frame.TypeMap)
                End If
            Else
                ' it must be a parameter
                Dim parameter = DirectCast(captured, ParameterSymbol)
                type = parameter.Type.InternalSubstituteTypeParameters(frame.TypeMap)
            End If

            Return type
        End Function

        Public Shared Function IsMe(captured As Symbol) As Boolean
            Dim parameter = TryCast(captured, ParameterSymbol)
            Return parameter IsNot Nothing AndAlso parameter.IsMe
        End Function

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return IsConst
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCapturedFrame As Boolean
            Get
                Return Me.m_isMe
            End Get
        End Property
    End Class

End Namespace