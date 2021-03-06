﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A region analysis walker that computes whether or not the region completes normally.  It does this by determining 
    ''' if the point at which the region ends is reachable.
    ''' </summary>
    Friend Class RegionReachableWalker
        Inherits AbstractRegionControlFlowPass

        Friend Overloads Shared Sub Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo,
                                            <Out()> ByRef startPointIsReachable As Boolean, <Out()> ByRef endPointIsReachable As Boolean)

            Dim walker = New RegionReachableWalker(info, region)
            Try
                If walker.Analyze() Then
                    startPointIsReachable = If(walker.regionStartPointIsReachable.HasValue, walker.regionStartPointIsReachable.Value, True)
                    endPointIsReachable = If(walker.regionEndPointIsReachable.HasValue, walker.regionEndPointIsReachable.Value, walker.State.Alive)
                Else
                    startPointIsReachable = True
                    startPointIsReachable = False
                End If
            Finally
                walker.Free()
            End Try
        End Sub

        Private regionStartPointIsReachable As Boolean?
        Private regionEndPointIsReachable As Boolean?

        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo)
            MyBase.New(info, region)
        End Sub

        Protected Overrides Sub EnterRegion()
            regionStartPointIsReachable = State.Alive
            MyBase.EnterRegion()
        End Sub

        Protected Overrides Sub LeaveRegion()
            regionEndPointIsReachable = State.Alive
            MyBase.LeaveRegion()
        End Sub

    End Class

End Namespace
