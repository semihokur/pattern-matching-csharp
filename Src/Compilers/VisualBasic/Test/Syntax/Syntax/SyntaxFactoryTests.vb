﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SyntaxFactoryTests

        <Fact>
        Public Sub SyntaxTreeFactory()
            Dim tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit(), encoding:=Nothing)
            Assert.Null(tree.GetText().Encoding)
        End Sub

        <Fact>
        Public Sub SyntaxTreeFromNode()
            Assert.Null(SyntaxFactory.CompilationUnit().SyntaxTree.GetText().Encoding)
        End Sub

        <Fact>
        <WorkItem(720708, "DevDiv")>
        Public Sub TestLiteralDefaultStringValues()

            ' string
            CheckLiteralToString("A", """A""")
            CheckLiteralToString(ChrW(7).ToString(), "ChrW(7)")
            CheckLiteralToString(ChrW(10).ToString(), "vbLf")

            ' char
            CheckLiteralToString("A"c, """A""c")
            CheckLiteralToString(ChrW(7), "ChrW(7)")
            CheckLiteralToString(ChrW(10), "vbLf")


            '' Unsupported in VB: byte, sbyte, ushort, short

            ' uint
            CheckLiteralToString(UInteger.MinValue, "0UI")
            CheckLiteralToString(UInteger.MaxValue, "4294967295UI")

            ' int
            CheckLiteralToString(0, "0")
            CheckLiteralToString(Integer.MinValue, "-2147483648")
            CheckLiteralToString(Integer.MaxValue, "2147483647")

            ' ulong
            CheckLiteralToString(ULong.MinValue, "0UL")
            CheckLiteralToString(ULong.MaxValue, "18446744073709551615UL")

            ' long
            CheckLiteralToString(0L, "0L")
            CheckLiteralToString(Long.MinValue, "-9223372036854775808L")
            CheckLiteralToString(Long.MaxValue, "9223372036854775807L")


            ' float
            CheckLiteralToString(0.0F, "0F")
            CheckLiteralToString(0.012345F, "0.012345F")
            CheckLiteralToString(Single.MaxValue, "3.40282347E+38F")

            ' double
            CheckLiteralToString(0.0, "0")
            CheckLiteralToString(0.012345, "0.012345")
            CheckLiteralToString(Double.MaxValue, "1.7976931348623157E+308")

            ' decimal
            CheckLiteralToString(0D, "0D")
            CheckLiteralToString(0.012345D, "0.012345D")
            CheckLiteralToString(Decimal.MaxValue, "79228162514264337593543950335D")
        End Sub

        Private Shared Sub CheckLiteralToString(value As Object, expected As String)
            Dim factoryType As System.Type = GetType(SyntaxFactory)
            Dim literalMethods = factoryType.GetMethods().Where(Function(m) m.Name = "Literal" AndAlso m.GetParameters().Count() = 1)
            Dim literalMethod = literalMethods.Single(Function(m) m.GetParameters().Single().ParameterType = value.GetType())

            Assert.Equal(expected, literalMethod.Invoke(Nothing, {value}).ToString())
        End Sub
    End Class
End Namespace