﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source
{
    public sealed class ExpressionBodiedMethodTests : CSharpTestBase
    {
        [Fact(Skip = "973907")]
        public void Syntax01()
        {
            // Feature is enabled by default
            var comp = CreateCompilationWithMscorlib(@"
class C
{
    public int M() => 1;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Syntax02()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public int M() {} => 1;
}");
            comp.VerifyDiagnostics(
    // (4,5): error CS8056: Methods cannot combine block bodies with expression bodies.
    //     public int M() {} => 1;
    Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "public int M() {} => 1;").WithLocation(4, 5),
    // (4,16): error CS0161: 'C.M()': not all code paths return a value
    //     public int M() {} => 1;
    Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(4, 16));
        }

        [Fact]
        public void Syntax03()
        {
            var comp = CreateCompilationWithMscorlib45(@"
interface C
{
    int M() => 1;
}");
            comp.VerifyDiagnostics(
    // (4,9): error CS0531: 'C.M()': interface members cannot have a definition
    //     int M() => 1;
    Diagnostic(ErrorCode.ERR_InterfaceMemberHasBody, "M").WithArguments("C.M()").WithLocation(4, 9));
        }

        [Fact]
        public void Syntax04()
        {
            var comp = CreateCompilationWithMscorlib45(@"
abstract class C
{
  public abstract int M() => 1;
}");
            comp.VerifyDiagnostics(
    // (4,23): error CS0500: 'C.M()' cannot declare a body because it is marked abstract
    //   public abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "M").WithArguments("C.M()").WithLocation(4, 23));
        }

        [Fact]
        public void Syntax05()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
   public abstract int M() => 1;
}");
            comp.VerifyDiagnostics(
    // (4,24): error CS0500: 'C.M()' cannot declare a body because it is marked abstract
    //    public abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "M").WithArguments("C.M()").WithLocation(4, 24),
    // (4,24): error CS0513: 'C.M()' is abstract but it is contained in non-abstract class 'C'
    //    public abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "M").WithArguments("C.M()", "C").WithLocation(4, 24));
        }

        [Fact]
        public void Syntax06()
        {
            var comp = CreateCompilationWithMscorlib45(@"
abstract class C
{
   abstract int M() => 1;
}");
            comp.VerifyDiagnostics(
    // (4,17): error CS0500: 'C.M()' cannot declare a body because it is marked abstract
    //    abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "M").WithArguments("C.M()").WithLocation(4, 17),
    // (4,17): error CS0621: 'C.M()': virtual or abstract members cannot be private
    //    abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_VirtualPrivate, "M").WithArguments("C.M()").WithLocation(4, 17));
        }


        [Fact]
        public void LambdaTest01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
using System;
class C
{
    public Func<int, Func<int, int>> M() => x => y => x + y;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SimpleTest()
        {
            var text = @"
class C
{
    public int P => 2;
    public int M() => P;
    public static explicit operator C(int i) => new C();
    public static C operator++(C c) => (C)c.M();
}";
            var comp = CreateCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics();
            var global = comp.GlobalNamespace;
            var c = global.GetTypeMember("C");

            var m = c.GetMember<SourceMethodSymbol>("M");
            Assert.False(m.IsImplicitlyDeclared);
            Assert.True(m.IsExpressionBodied);

            var pp = c.GetMember<SourceUserDefinedOperatorSymbol>("op_Increment");
            Assert.False(pp.IsImplicitlyDeclared);
            Assert.True(pp.IsExpressionBodied);

            var conv = c.GetMember<SourceUserDefinedConversionSymbol>("op_Explicit");
            Assert.False(conv.IsImplicitlyDeclared);
            Assert.True(conv.IsExpressionBodied);
        }

        [Fact]
        public void Override01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class B
{
    public virtual int M() { return 0; }
}
class C : B
{
    public override int M() => 1;
}").VerifyDiagnostics();
        }

        [Fact]
        public void VoidExpression()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public void M() => System.Console.WriteLine(""foo"");
}").VerifyDiagnostics();
        }

        [Fact]
        public void VoidExpression2()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public int M() => System.Console.WriteLine(""foo"");
}").VerifyDiagnostics(
    // (4,23): error CS0029: Cannot implicitly convert type 'void' to 'int'
    //     public int M() => System.Console.WriteLine("foo");
    Diagnostic(ErrorCode.ERR_NoImplicitConv, @"System.Console.WriteLine(""foo"")").WithArguments("void", "int").WithLocation(4, 23));
        }

        [Fact]
        public void InterfaceImplementation01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
interface I 
{
    int M();
    string N();
}
internal interface J
{
    string N();
}
internal interface K
{
    decimal O();
}
class C : I, J, K
{
    public int M() => 10;
    string I.N() => ""foo"";
    string J.N() => ""bar"";
    public decimal O() => M();
}");
            comp.VerifyDiagnostics();
            var global = comp.GlobalNamespace;
            var i = global.GetTypeMember("I");
            var j = global.GetTypeMember("J");
            var k = global.GetTypeMember("K");
            var c = global.GetTypeMember("C");

            var iM = i.GetMember<SourceMethodSymbol>("M");
            var iN = i.GetMember<SourceMethodSymbol>("N");
            var jN = j.GetMember<SourceMethodSymbol>("N");

            var method = c.GetMember<SourceMethodSymbol>("M");
            var implements = method.ContainingType.FindImplementationForInterfaceMember(iM);
            Assert.Equal(implements, method);

            method = c.GetMember<SourceMethodSymbol>("I.N");
            implements = c.FindImplementationForInterfaceMember(iN);
            Assert.True(method.IsExplicitInterfaceImplementation);
            Assert.Equal(implements, method);

            method = c.GetMember<SourceMethodSymbol>("J.N");
            implements = c.FindImplementationForInterfaceMember(jN);
            Assert.True(method.IsExplicitInterfaceImplementation);
            Assert.Equal(implements, method);

            method = c.GetMember<SourceMethodSymbol>("O");
            Assert.False(method.IsExplicitInterfaceImplementation);
        } 

        [Fact]
        public void Emit01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
abstract class A
{
    protected abstract string Z();
}
abstract class B : A
{
    protected sealed override string Z() => ""foo"";
    protected abstract string Y();
}    
class C : B
{
    public const int X = 2;
    public static int M(int x) => x * x;
    
    public int N() => X;
    private int O() => M(N()) * N();
    protected sealed override string Y() => Z() + O();

    public static void Main()
    {
        System.Console.WriteLine(C.X);
        System.Console.WriteLine(C.M(C.X));
        var c = new C();
        
        System.Console.WriteLine(c.N());
        System.Console.WriteLine(c.O());
        System.Console.WriteLine(c.Z());
        System.Console.WriteLine(c.Y());
    }
}", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal));
            var verifier = CompileAndVerify(comp, expectedOutput:
@"2
4
2
8
foo
foo8");
        }

        [Fact]
        public void Emit02()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public void M() { System.Console.WriteLine(""Hello""); }
    public void M(int i) { System.Console.WriteLine(i); }

    public string N(string s) { return s; }

    public static void Main()
    {
        var c = new C();
        c.M();
        c.M(2);

        System.Console.WriteLine(c.N(""World""));
    }
}", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal));
            var verifier = CompileAndVerify(comp, expectedOutput:
@"Hello
2
World");
        }
    }
}
