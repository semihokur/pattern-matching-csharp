// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests : CSharpTestBase
    {
        [Fact]
        public void MatchExpressionConstantPattern()
        {
            var source = @"
using System;
class Program
{
    enum Days { Sun, Mon, Tue }
    static void Main(string[] args)
    {
        // Object to Literals
        object o;
        o = (int)2;
        if (o is 2)
            Console.WriteLine(""int 2"");

        o = (uint)2;
        if (o is 2)
            Console.WriteLine(""uint 2"");

        o = (double)2.5;
        if (o is 2.5)
            Console.WriteLine(""double 2.5"");

        o = (float)2.5;
        if (o is 2.5)
            Console.WriteLine(""float 2.5"");

        o = (string)""2"";
        if (o is ""2"")
            Console.WriteLine(""string 2"");

        o = Days.Mon;
        if (o is Days.Mon)
            Console.WriteLine(""enum Days.Mon"");

        o = true;
        if (o is true)
            Console.WriteLine(""bool true"");

        o = (int)2;
        if (o is ""2"")
            Console.WriteLine(""FAIL"");
        else if (o is Days.Sun)
            Console.WriteLine(""FAIL"");
        else if (o is 3.5)
            Console.WriteLine(""FAIL"");

        // Constant variables and binary expression
        const int const2 = 2;
        o = (int)2;
        if (o is const2)
            Console.WriteLine(""const""+ const2);

        if (o is const2 + const2 - const2)
            Console.WriteLine(""const2 + const2 - const2"");

        if (o is -(-const2))
            Console.WriteLine(""-(-const2)"");

        if (o is 1 + 1)
            Console.WriteLine(""1+1"");

        // Constant expressions
        if (o is sizeof(int))
            Console.WriteLine(""FAIL"");

        string b = ""b"";
        if (b is nameof(b))
            Console.WriteLine(""nameof(b)"");

        // null 
        Test t = null;
        if (t is null)
            Console.WriteLine(""null"");

        int a = 3;
        if (a is null)
            Console.WriteLine(""FAIL"");

        // parsed as type
        bool test = t is Test;
        test = t is Program.Test;
        test = o is int;
        test = o is int?;
    }
    class Test { }
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            var verifier = CompileAndVerify(compilation, expectedOutput: @"
int 2
uint 2
double 2.5
float 2.5
string 2
enum Days.Mon
bool true
const2
const2 + const2 - const2
-(-const2)
1+1
nameof(b)
null").VerifyDiagnostics();
        }


        [Fact]
        public void MatchStatementConstantPattern()
        {
            var source = @"
using System;
class Program
{
    enum Days { Sun, Mon, Tue }
    const int const2 = 2;
    static void Main(string[] args)
    {
        MatchConstant((object)2);
        MatchConstant(Days.Mon);
        MatchConstant((float)2.5);
        object o=null;
        MatchConstant(o);
        MatchConstant(true);
        MatchConstant(sizeof(int));
    }
    static void MatchConstant(object o)
    {
        switch (o)
        {
            case 2:
                Console.WriteLine(""int 2"");
                break;
            case 2.5:
                Console.WriteLine(""double 2.5"");
                break;
            case ""2"":
                Console.WriteLine(""string 2"");
                break;
            case Days.Mon:
                Console.WriteLine(""Days.Mon"");
                break;
            case true:
                Console.WriteLine(""bool true"");
                break;
            case null:
                Console.WriteLine(""null"");
                break;
            case const2:
                Console.WriteLine(""const2"");
                break;
            case const2 + const2 - const2:
                Console.WriteLine(""const2 + const2 - const2"");
                break;
            case 1 + 1:
                Console.WriteLine(""1+1"");
                break;
            case sizeof(int):
                Console.WriteLine(""sizeof(int)"");
                break;
            case nameof(o):
                Console.WriteLine(""nameof(o)"");
                break;
            case int a :
                Console.WriteLine("""");
                break;
        }
    }
    class Test { }
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            var verifier = CompileAndVerify(compilation, expectedOutput: @"
int 2
Days.Mon
double 2.5
null
bool true
sizeof(int)").VerifyDiagnostics();
        }

        [Fact]
        public void MatchExpressionStatementSimplePatternDiagnostics()
        {
            var source = @"
using System;
class Program
{
    enum Days { Sun, Mon, Tue }
    static void Main(string[] args)
    {
        object o = null;
        int b = 3;
        switch (b)
        {
            case o: break;
            case 2.5: break;
            case ""2"": break;
            case Days.Sun: break;
            case null: break;
            case int s : break;
            case string f: break;
            case Super a : break;
            case typeof(int): break;
            case string: break;
        }
        Sub sub = null;
        Super super = null;
        if (sub is Super s2)
            Console.Write(s2);
        if (super is Sub s2)
            Console.Write(s2);

        bool r = o is int;
        r = o is b++;
        r = o is b;
        r = b is 4.5;
        r = b is ""3"";
    }
    class Super { }
    class Sub : Super { }
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            compilation.VerifyDiagnostics(
                // (21,24): error CS1001: Identifier expected
                //             case string: break;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(21, 24),
                // (31,19): error CS1002: ; expected
                //         r = o is b++;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "++").WithLocation(31, 19),
                // (31,21): error CS1525: Invalid expression term ';'
                //         r = o is b++;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(31, 21),
                // (12,18): error CS0150: A constant value is expected
                //             case o: break;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "o").WithLocation(12, 18),
                // (13,18): error CS0029: Cannot implicitly convert type 'double' to 'int'
                //             case 2.5: break;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "2.5").WithArguments("double", "int").WithLocation(13, 18),
                // (14,18): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //             case "2": break;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""2""").WithArguments("string", "int").WithLocation(14, 18),
                // (15,18): error CS0029: Cannot implicitly convert type 'Program.Days' to 'int'
                //             case Days.Sun: break;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Days.Sun").WithArguments("Program.Days", "int").WithLocation(15, 18),
                // (18,18): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //             case string f: break;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "string f").WithArguments("string", "int").WithLocation(18, 18),
                // (19,18): error CS0029: Cannot implicitly convert type 'Program.Super' to 'int'
                //             case Super a : break;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Super a").WithArguments("Program.Super", "int").WithLocation(19, 18),
                // (20,18): error CS0150: A constant value is expected
                //             case typeof(int): break;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "typeof(int)").WithLocation(20, 18),
                // (21,18): error CS8047: A declaration expression is not permitted in this context.
                //             case string: break;
                Diagnostic(ErrorCode.ERR_DeclarationExpressionOutOfContext, "string").WithLocation(21, 18),
                // (25,20): error CS0029: Cannot implicitly convert type 'Program.Super' to 'Program.Sub'
                //         if (sub is Super s2)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Super s2").WithArguments("Program.Super", "Program.Sub").WithLocation(25, 20),
                // (31,18): error CS0118: 'b' is a variable but is used like a type
                //         r = o is b++;
                Diagnostic(ErrorCode.ERR_BadSKknown, "b").WithArguments("b", "variable", "type").WithLocation(31, 18),
                // (32,18): error CS0118: 'b' is a variable but is used like a type
                //         r = o is b;
                Diagnostic(ErrorCode.ERR_BadSKknown, "b").WithArguments("b", "variable", "type").WithLocation(32, 18),
                // (33,18): error CS0029: Cannot implicitly convert type 'double' to 'int'
                //         r = b is 4.5;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "4.5").WithArguments("double", "int").WithLocation(33, 18),
                // (34,18): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         r = b is "3";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""3""").WithArguments("string", "int").WithLocation(34, 18));

        }

        [Fact]
        public void MatchStatementAndExpression_Patterns_WithoutRecords()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        object o = (int)2;
        Expr exp = new Add(new Add(new Const(3), new Const(4)), new Const(5));
        Verify(3.5);
        Verify(""2"");
        Verify(new Const(31));
        Verify(new Const(100));
        Verify(new Add(new Const(2), new Const(2)));
        Verify(new Add(new Add(new Const(2), new Const(0)), new Const(3)));
        Verify(new Add(new Add(new Const(1), new Const(2)),
                       new Add(new Const(0), new Const(1231))));
        Verify(new Add(new Add(new Const(1), new Const(2)),
                       new Add(new Const(123), new Const(3))));
        Verify(new Add(new Add(new Const(31), new Const(322)),
                       new Add(new Const(123), new Const(322))));
    }

    static void Verify(object o)
    {
        bool r = MatchExpressions(o) == MatchStatements(o);
        Console.WriteLine(r + "" "" + MatchStatements(o));
    }
    static string MatchStatements(object o)
    {
        switch (o)
        { // from specific to more general:
            case Add(Add(Const(1), Const(2)), Add(Const(0), Const(int z))) :
                return ""(1 + 2) + (0 + "" + z + "")"";
            case Add(Add(Const(1), Const(2)), Add(Const(*), Const(int u))) :
                return ""(1 + 2) + (x + "" + u + "")"";
            case Add(var x, Const(2)) :
                return x + "" + 2"";
            case Add(var x, Const(*)) :
                return x + "" + const"";
            case Const(100) :
                return ""const 100"";
            case Const(int e) :
                return ""const "" + e;
            case Const(*) :
                return ""const x"";
            case Add a :
                return a.Left + "" + "" + a.right;
            case double d :
                return ""double "" + d;
            default:
                return ""None"";
        }
    }
    static string MatchExpressions(object o)
    {
        if (o is Add(Add(Const(1), Const(2)), Add(Const(0), Const(int z))))
            return ""(1 + 2) + (0 + "" + z + "")"";
        else if (o is Add(Add(Const(1), Const(2)), Add(Const(*), Const(int u))))
            return ""(1 + 2) + (x + "" + u + "")"";
        else if (o is Add(var x, Const(2)))
            return x + "" + 2"";
        else if (o is Add(var x, Const(*)))
            return x + "" + const"";
        else if (o is Const(100))
            return ""const 100"";
        else if (o is Const(int e))
            return ""const "" + e;
        else if (o is Const(*))
            return ""const x"";
        else if (o is Add a)
            return a.Left + "" + "" + a.right;
        else if (o is double d)
            return ""double "" + d;
        else
            return ""None"";
    }
}

public abstract class Expr { }
public class Const(int x) : Expr
{
    public int X { get; } = x;
    public static bool operator is(Const a, out int x)
    {
        x = a.X;
        return true;
    }
}
public class Add(Expr left, Expr right) : Expr
{
    public Expr Left { get; } = left;
    public Expr right { get; } = right;
    public static bool operator is(Add a, out Expr left, out Expr right)
    {
        left = a.Left;
        right = a.right;
        return true;
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            var verifier = CompileAndVerify(compilation, expectedOutput: @"
True double 3.5
True None
True const 31
True const 100
True Const + 2
True Add + const
True (1 + 2) + (0 + 1231)
True (1 + 2) + (x + 3)
True Add + Add").VerifyDiagnostics();
        }

        [Fact]
        public void MatchStatementAndExpression_Patterns_WithRecords()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        object o = (int)2;
        Expr exp = new Add(new Add(new Const(3), new Const(4)), new Const(5));
        Verify(3.5);
        Verify(""2"");
        Verify(new Const(31));
        Verify(new Const(100));
        Verify(new Add(new Const(2), new Const(2)));
        Verify(new Add(new Add(new Const(2), new Const(0)), new Const(3)));
        Verify(new Add(new Add(new Const(1), new Const(2)),
                       new Add(new Const(0), new Const(1231))));
        Verify(new Add(new Add(new Const(1), new Const(2)),
                       new Add(new Const(123), new Const(3))));
        Verify(new Add(new Add(new Const(31), new Const(322)),
                       new Add(new Const(123), new Const(322))));
    }

    static void Verify(object o)
    {
        bool r = MatchExpressions(o) == MatchStatements(o);
        Console.WriteLine(r + "" "" + MatchStatements(o));
    }
    static string MatchStatements(object o)
    {
        switch (o)
        { // from specific to more general:
            case Add(Add(Const(1), Const(2)), Add(Const(0), Const(int z))) :
                return ""(1 + 2) + (0 + "" + z + "")"";
            case Add(Add(Const(1), Const(2)), Add(Const(*), Const(int u))) :
                return ""(1 + 2) + (x + "" + u + "")"";
            case Add(var x, Const(2)) :
                return x + "" + 2"";
            case Add(var x, Const(*)) :
                return x + "" + const"";
            case Const(100) :
                return ""const 100"";
            case Const(int e) :
                return ""const "" + e;
            case Const(*) :
                return ""const x"";
            case Add a :
                return a.Left + "" + "" + a.right;
            case double d :
                return ""double "" + d;
            default:
                return ""None"";
        }
    }
    static string MatchExpressions(object o)
    {
        if (o is Add(Add(Const(1), Const(2)), Add(Const(0), Const(int z))))
            return ""(1 + 2) + (0 + "" + z + "")"";
        else if (o is Add(Add(Const(1), Const(2)), Add(Const(*), Const(int u))))
            return ""(1 + 2) + (x + "" + u + "")"";
        else if (o is Add(var x, Const(2)))
            return x + "" + 2"";
        else if (o is Add(var x, Const(*)))
            return x + "" + const"";
        else if (o is Const(100))
            return ""const 100"";
        else if (o is Const(int e))
            return ""const "" + e;
        else if (o is Const(*))
            return ""const x"";
        else if (o is Add a)
            return a.Left + "" + "" + a.right;
        else if (o is double d)
            return ""double "" + d;
        else
            return ""None"";
    }
}
public abstract class Expr { }
public record class Const(int x : X) : Expr { }
public record class Add(Expr left : Left, Expr right) : Expr { }
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            var verifier = CompileAndVerify(compilation, expectedOutput: @"
True double 3.5
True None
True const 31
True const 100
True Const + 2
True Add + const
True (1 + 2) + (0 + 1231)
True (1 + 2) + (x + 3)
True Add + Add").VerifyDiagnostics();
        }

        [Fact]
        public void CartesianPolar_CustomIsOperator()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        var c = new Cartesian(3, 4);
        if (c is Polar(double e, var d))
            Console.WriteLine(""Cartesian ""+e +"", "" +d);
        c = new Cartesian(6, 8);
        if (c is Polar(10.0, double d))
            Console.WriteLine(""Tan: ""+d);
    }
}
public class Polar
{
    public static bool operator is(Cartesian c, out double R, out double Theta)
    {
        R = Math.Sqrt(c.X * c.X + c.Y * c.Y);
        Theta = Math.Atan2(c.Y, c.X);
        return c.X != 0 || c.Y != 0;
    }
}
public record class Cartesian(double x :X, double y :Y) { }
";

                var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                    parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

                var verifier = CompileAndVerify(compilation, expectedOutput: @"
Cartesian 5, 0.927295218001612
Tan: 0.927295218001612").VerifyDiagnostics();
            }

        [Fact]
        public void ExpressionSimplification_NamedPatterns_WithRecords()
        {
            var source = @"

using System;
class Program
{
    static void Main(string[] args)
    {
        // exp = (0 * (1 + 2)) + 0
        Expr exp = new Add(new Mult(new Const(0),
                                    new Add(new Const(1),
                                            new Const(2))),
                           new Const(0));
        Expr expected = new Const(0);
        Console.WriteLine(""Depth: "" + depth(exp));
        exp = Simplify(exp);
        Console.WriteLine(""Depth after simp: "" + depth(exp) + "" isCorrect: "" + exp.Equals(expected));
        // exp = (0 + (1 + (2 * 0))) + (1 * (0 + (2 * 0)))
        exp = new Add(new Add(new Const(0),
                              new Add(new Const(1),
                                      new Mult(new Const(2),
                                               new Const(0)))),
                        new Mult(new Const(1),
                                    new Add(new Const(0),
                                            new Mult(new Const(2),
                                                     new Const(0)))));
        expected = new Const(1);
        Console.WriteLine(""Depth: "" + depth(exp));
        exp = Simplify(exp);
        Console.WriteLine(""Depth after simp: "" + depth(exp) + "" isCorrect: "" + exp.Equals(expected));
    }

    static int depth(Expr e)
    {
        switch (e)
        {
            case Add(var leftAdd, right: var rightAdd) : return 1 + Math.Max(depth(leftAdd), depth(rightAdd));
            case Mult(left: var leftMult, right: var rightMult) : return 1 + Math.Max(depth(leftMult), depth(rightMult));
            default: return 0;
        }
    }
    static Expr Simplify(Expr e)
    {
        Expr newNode = null;
        switch (e)
        {
            case Const c : return c;
            case Mult(Const(x: 0), right: *) :
            case Mult(right: Const(0), left: *) : return new Const(0);
            case Add(Const(0), right: var right) : return Simplify(right);
            case Add(var left, Const(x: 0)) : return Simplify(left);
            case Mult mult :
                newNode = new Mult(Simplify(mult.Left), Simplify(mult.Right));
                break;
            case Add add :
                newNode = new Add(Simplify(add.Left), Simplify(add.Right));
        }
        Expr simp = Simplify(newNode);
        return simp.Equals(newNode) ? newNode : Simplify(simp);
    }
}
public abstract class Expr { }
public record class Const(int x : X) : Expr { }
public record class Add(Expr left : Left, Expr right : Right) : Expr { }
public record class Mult(Expr left : Left, Expr right : Right) : Expr { }

";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            var verifier = CompileAndVerify(compilation, expectedOutput: @"
Depth: 3
Depth after simp: 0 isCorrect: True
Depth: 4
Depth after simp: 0 isCorrect: True").VerifyDiagnostics();
        }

        [Fact]
        public void Generated_NamedProperties_GetHashCode_Equals_RecordsTest()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
		Add addExpr1 = new Add(new Const(0), new Const(0));
		Mult multExpr1 = new Mult(new Const(0), new Const(0));

		Const constE = (Const) addExpr1.Left;
		int x = constE.X; 
		Expr right = addExpr1.right;
		right = multExpr1.Right;
		constE = (Const)multExpr1.left;

		Add addExpr2 = new Add(new Const(0), new Const(0));
		if(addExpr1.Equals(addExpr2))
			Console.WriteLine(""Equal"");
		if(addExpr2.Equals(addExpr1))
			Console.WriteLine(""Equal"");		
		addExpr2 = new Add(new Const(0), new Const(1));
		if(addExpr1.GetHashCode() != addExpr2.GetHashCode())
			Console.WriteLine(""Not Equal"");	
    }
}
public abstract class Expr { }
public record class Const (int x : X) : Expr {}
public record class Add (Expr left: Left, Expr right) : Expr {}
public record class Mult (Expr left, Expr right: Right) : Expr {}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            var verifier = CompileAndVerify(compilation, expectedOutput: @"
Equal
Equal
Not Equal").VerifyDiagnostics();
        }

        [Fact]
        public void OverloadResolutionForOpIs()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        // exp = (0 * (1 + 2))
        Expr exp = new Mult(new Const(0), new Add(new Const(1), new Const(2)));
        if (3 is Const(Const f))
            Console.WriteLine(f.X);
        if (exp is Add(right: double d, left: int i))
            Console.WriteLine(i+""+""+d);
        if (exp is Mult(right: Add e, left: Const(0)))
            Console.WriteLine(e);
        if (exp is Mult(Const(0), Add(Test t, Const e)))
            Console.WriteLine(t+""*""+e.X);
    }
}
public class Test { }
public abstract class Expr { }
public record class Const(int x : X) : Expr
{
    public static bool operator is(Const e, out int x)
    {
        x = e.X;
        return true;
    }
    public static bool operator is(Const e, out Expr f)
    {
        f = null;
        return true;
    }
    public static bool operator is(int e, out Const f)
    {
        f = new Const(3);
        return true;
    }
}
public record class Add(Expr left, Expr right) : Expr
{
    public static bool operator is(Add e, out Expr left, out Expr right)
    {
        left = e.left;
        right = e.right;
        return true;
    }
    public static bool operator is(int e, out Expr left, out Expr right)
    {
        left = new Const(31);
        right = new Const(31);
        return true;
    }
    public static bool operator is(Test e, out Test left, out Expr right)
    {
        left = new Test();
        right = new Const(31);
        return true;
    }
    public static bool operator is(int e, out Test left, out Expr right)
    {
        left = new Test();
        right = new Const(3);
        return true;
    }
    public static bool operator is(Expr e, out int left, out double right)
    {
        left = 2;
        right = 3;
        return true;
    }
}
public record class Mult(Expr left, Expr right) : Expr
{
    public static bool operator is(Mult e, out Expr left, out Expr right)
    {
        left = e.left;
        right = e.right;
        return true;
    }

    public static bool operator is(Mult e, out Expr left, out Test right)
    {
        left = e.left;
        right = new Test();
        return true;
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            var verifier = CompileAndVerify(compilation, expectedOutput: @"
3
2+3
Add
Test*31").VerifyDiagnostics();
        }

        [Fact]
        public void OpIsTypeCheckingDiagnosis()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        // exp = (0 * (1 + 2))
        Expr exp = new Mult(new Const(0), new Add(new Const(1), new Const(2)));
        bool b= 3 is Const(Const f, int y));
        b = exp is Const(f: Test());
        b = exp is Const(g: *);
        b = exp is Mult(Const(0), Add(Const(int s), Const(2)));

        // should give an error. right is not Test in all opIs
        b = exp is Add(right: Test c, left: *);
        b = exp is Add(*, Test t2);
    }
}
public class Test { }
public abstract class Expr { }
public record class Const(int x : X) : Expr
{
    public static bool operator is(Const e, out int x)
    {
        x = e.X;
        return true;
    }
    public static bool operator is(Const e, out Expr f)
    {
        f = null;
        return true;
    }
    public static bool operator is(int e, out Const f)
    {
        f = new Const(3);
        return true;
    }
}
public record class Add(Expr left, Expr right) : Expr
{
    public static bool operator is(int e, out Expr left, out Expr right)
    {
        left = new Const(31);
        right = new Const(31);
        return true;
    }
    public static bool operator is(Test e, out Test left, out Expr right)
    {
        left = new Test();
        right = new Const(31);
        return true;
    }
    public static bool operator is(int e, out Test left, out Expr right)
    {
        left = new Test();
        right = new Const(3);
        return true;
    }
    public static bool operator is(Expr e, out int left, out double right)
    {
        left = 2;
        right = 3;
        return true;
    }
}
public record class Mult(Expr left, Expr right) : Expr
{
    public static bool operator is(Mult e, out Expr left, out Expr right)
    {
        left = e.left;
        right = e.right;
        return true;
    }

    public static bool operator is(Mult e, out Expr left, out Test right)
    {
        left = e.left;
        right = new Test();
        return true;
    }
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            compilation.VerifyDiagnostics(
                // (9,43): error CS1002: ; expected
                //         bool b= 3 is Const(Const f, int y));
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(9, 43),
                // (9,43): error CS1513: } expected
                //         bool b= 3 is Const(Const f, int y));
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(9, 43),
                // (9,22): error CS0117: 'Const' does not contain a definition for 'is operator'
                //         bool b= 3 is Const(Const f, int y));
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Const(Const f, int y)").WithArguments("Const", "is operator").WithLocation(9, 22),
                // (10,29): error CS0117: 'Test' does not contain a definition for 'is operator'
                //         b = exp is Const(f: Test());
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Test()").WithArguments("Test", "is operator").WithLocation(10, 29),
                // (11,20): error CS0117: 'Const' does not contain a definition for 'is operator'
                //         b = exp is Const(g: *);
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Const(g: *)").WithArguments("Const", "is operator").WithLocation(11, 20),
                // (12,35): error CS0029: Cannot implicitly convert type 'int' to 'Test'
                //         b = exp is Mult(Const(0), Add(Const(int s), Const(2)));
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Add").WithArguments("int", "Test").WithLocation(12, 35),
                // (15,31): error CS0029: Cannot implicitly convert type 'Test' to 'double'
                //         b = exp is Add(right: Test c, left: *);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Test c").WithArguments("Test", "double").WithLocation(15, 31),
                // (16,27): error CS0029: Cannot implicitly convert type 'Test' to 'double'
                //         b = exp is Add(*, Test t2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Test t2").WithArguments("Test", "double").WithLocation(16, 27),
                // (9,34): warning CS0168: The variable 'f' is declared but never used
                //         bool b= 3 is Const(Const f, int y));
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "f").WithArguments("f").WithLocation(9, 34),
                // (9,41): warning CS0168: The variable 'y' is declared but never used
                //         bool b= 3 is Const(Const f, int y));
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "y").WithArguments("y").WithLocation(9, 41),
                // (12,49): warning CS0168: The variable 's' is declared but never used
                //         b = exp is Mult(Const(0), Add(Const(int s), Const(2)));
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "s").WithArguments("s").WithLocation(12, 49),
                // (15,36): warning CS0168: The variable 'c' is declared but never used
                //         b = exp is Add(right: Test c, left: *);
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "c").WithArguments("c").WithLocation(15, 36),
                // (16,32): warning CS0168: The variable 't2' is declared but never used
                //         b = exp is Add(*, Test t2);
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "t2").WithArguments("t2").WithLocation(16, 32),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(2, 1));

        }

        [Fact]
        public void PropertyPattern()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {

        Expr exp = new Const(3);

        if (exp is Const {X is 3})
            Console.WriteLine(""OK"");

        if (exp is Const{X is var s})
            Console.WriteLine(s);

        exp = new Add(new Const(0), new Const(2));

        if (exp is Add(Const{ X is 0}, Const(var s)))
            Console.WriteLine(s);
		
		if(exp is Add { Left is Const(0)})
            Console.WriteLine(""OK"");
		
		if(exp is Add { Left is Const { X is 0}, right is Const(2) })
            Console.WriteLine(""OK"");
    }
}
public abstract class Expr { }
public record class Const(int x : X) : Expr { }
public record class Add(Expr left : Left, Expr right) : Expr { }
";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental).WithPreprocessorSymbols("RECORDS"));

            var verifier = CompileAndVerify(compilation, expectedOutput: @"
OK
3
2
OK
OK").VerifyDiagnostics();
        }
    }
}
