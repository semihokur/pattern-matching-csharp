﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A <see cref="MissingAssemblySymbol"/> is a special kind of <see cref="AssemblySymbol"/> that represents
    ''' an assembly that couldn't be found.
    ''' </summary>
    Friend Class MissingAssemblySymbol
        Inherits AssemblySymbol

        Protected ReadOnly m_Identity As AssemblyIdentity
        Protected ReadOnly m_ModuleSymbol As MissingModuleSymbol

        Private m_LazyModules As ImmutableArray(Of ModuleSymbol)

        Public Sub New(identity As AssemblyIdentity)
            Debug.Assert(identity IsNot Nothing)
            m_Identity = identity
            m_ModuleSymbol = New MissingModuleSymbol(Me, 0)
        End Sub

        Friend NotOverridable Overrides ReadOnly Property IsMissing As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property IsLinked As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetDeclaredSpecialTypeMember(member As SpecialMember) As Symbol
            Return Nothing
        End Function

        Public Overrides ReadOnly Property Identity As AssemblyIdentity
            Get
                Return m_Identity
            End Get
        End Property

        Friend Overrides ReadOnly Property PublicKey As ImmutableArray(Of Byte)
            Get
                Return Identity.PublicKey
            End Get
        End Property

        Public Overrides ReadOnly Property Modules As ImmutableArray(Of ModuleSymbol)
            Get
                If m_LazyModules.IsDefault Then
                    m_LazyModules = ImmutableArray.Create(Of ModuleSymbol)(m_ModuleSymbol)
                End If

                Return m_LazyModules
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property GlobalNamespace As NamespaceSymbol
            Get
                Return m_ModuleSymbol.GlobalNamespace
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return m_Identity.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, MissingAssemblySymbol))
        End Function

        Public Overloads Function Equals(other As MissingAssemblySymbol) As Boolean
            Return other IsNot Nothing AndAlso (Me Is other OrElse m_Identity.Equals(other.m_Identity))
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Friend Overrides Sub SetLinkedReferencedAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides Function GetLinkedReferencedAssemblies() As ImmutableArray(Of AssemblySymbol)
            Return ImmutableArray(Of AssemblySymbol).Empty
        End Function

        Friend Overrides Sub SetNoPiaResolutionAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides Function GetNoPiaResolutionAssemblies() As ImmutableArray(Of AssemblySymbol)
            Return ImmutableArray(Of AssemblySymbol).Empty
        End Function

        Friend Overrides Function GetInternalsVisibleToPublicKeys(simpleName As String) As IEnumerable(Of ImmutableArray(Of Byte))
            Return SpecializedCollections.EmptyEnumerable(Of ImmutableArray(Of Byte))()
        End Function

        Public Overrides ReadOnly Property TypeNames As ICollection(Of String)
            Get
                Return SpecializedCollections.EmptyCollection(Of String)()
            End Get
        End Property

        Public Overrides ReadOnly Property NamespaceNames As ICollection(Of String)
            Get
                Return SpecializedCollections.EmptyCollection(Of String)()
            End Get
        End Property

        Friend Overrides Function AreInternalsVisibleToThisAssembly(other As AssemblySymbol) As Boolean
            Return False
        End Function

        Friend Overrides Function LookupTopLevelMetadataTypeWithCycleDetection(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol), digThroughForwardedTypes As Boolean) As NamedTypeSymbol
            Dim result = m_ModuleSymbol.LookupTopLevelMetadataType(emittedName)
            Debug.Assert(TypeOf result Is MissingMetadataTypeSymbol)
            Return result
        End Function

        Friend Overrides Function GetDeclaredSpecialType(type As SpecialType) As NamedTypeSymbol
            Throw ExceptionUtilities.Unreachable
        End Function

        Public NotOverridable Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

    ''' <summary>
    ''' AssemblySymbol to represent missing, for whatever reason, CorLibrary.
    ''' The symbol is created by ReferenceManager on as needed basis and is shared by all compilations
    ''' with missing CorLibraries.
    ''' </summary>
    Friend NotInheritable Class MissingCorLibrarySymbol
        Inherits MissingAssemblySymbol

        Friend Shared ReadOnly Instance As MissingCorLibrarySymbol = New MissingCorLibrarySymbol()

        ''' <summary>
        ''' An array of cached Cor types defined in this assembly.
        ''' Lazily filled by GetDeclaredSpecialType method.
        ''' </summary>
        Private m_LazySpecialTypes() As NamedTypeSymbol

        Private Sub New()
            MyBase.New(New AssemblyIdentity("<Missing Core Assembly>"))
            Me.SetCorLibrary(Me)
        End Sub

        ''' <summary>
        ''' Lookup declaration for predefined CorLib type in this Assembly. Only should be
        ''' called if it is know that this is the Cor Library (mscorlib).
        ''' </summary>
        ''' <param name="type"></param>
        Friend Overrides Function GetDeclaredSpecialType(type As SpecialType) As NamedTypeSymbol
#If DEBUG Then
            For Each [module] In Me.Modules
                Debug.Assert([module].GetReferencedAssemblies().Length = 0)
            Next
#End If

            If m_LazySpecialTypes Is Nothing Then
                Interlocked.CompareExchange(m_LazySpecialTypes, New NamedTypeSymbol(SpecialType.Count) {}, Nothing)
            End If

            If m_LazySpecialTypes(type) Is Nothing Then
                Dim emittedFullName As MetadataTypeName = MetadataTypeName.FromFullName(SpecialTypes.GetMetadataName(type), useCLSCompliantNameArityEncoding:=True)
                Dim corType As NamedTypeSymbol = New MissingMetadataTypeSymbol.TopLevel(m_ModuleSymbol, emittedFullName, type)
                Interlocked.CompareExchange(m_LazySpecialTypes(type), corType, Nothing)
            End If

            Return m_LazySpecialTypes(type)

        End Function
    End Class

End Namespace