﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Design;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CA1008FixerTests : CodeFixTestBase
    {
        protected override IDiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new CA1008DiagnosticAnalyzer();
        }

        protected override ICodeFixProvider GetBasicCodeFixProvider()
        {
            return new CA1008BasicCodeFixProvider();
        }

        protected override IDiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CA1008DiagnosticAnalyzer();
        }

        protected override ICodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CA1008CSharpCodeFixProvider();
        }

        [WorkItem(836193)]
        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumsShouldZeroValueFlagsRename()
        {
            var code = @"
[System.Flags]
private enum E
{
    A = 0,
    B = 3
}

[System.Flags]
public enum E2
{
    A2 = 0,
    B2 = 1
}

[System.Flags]
public enum E3
{
    A3 = (ushort)0,
    B3 = (ushort)1
}

[System.Flags]
public enum E4
{
    A4 = 0,
    B4 = (uint)2  // Not a constant
}

[System.Flags]
public enum NoZeroValuedField
{
    A5 = 1,
    B5 = 2
}";

            var expectedFixedCode = @"
[System.Flags]
private enum E
{
    None,
    B = 3
}

[System.Flags]
public enum E2
{
    None,
    B2 = 1
}

[System.Flags]
public enum E3
{
    None,
    B3 = (ushort)1
}

[System.Flags]
public enum E4
{
    None,
    B4 = (uint)2  // Not a constant
}

[System.Flags]
public enum NoZeroValuedField
{
    A5 = 1,
    B5 = 2
}";
            VerifyCSharpFix(code, expectedFixedCode);
        }

        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumsShouldZeroValueFlagsMultipleZero()
        {
            var code = @"// Some comment
[System.Flags]
private enum E
{
    None = 0,
    A = 0
}
// Some comment
[System.Flags]
internal enum E2
{
    None = 0,
    A = None
}";
            var expectedFixedCode = @"// Some comment
[System.Flags]
private enum E
{
    None = 0
}
// Some comment
[System.Flags]
internal enum E2
{
    None = 0
}";
            VerifyCSharpFix(code, expectedFixedCode);
        }

        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumsShouldZeroValueFlagsMultipleZeroWithScope()
        {
            var code = @"// Some comment
[System.Flags]
private enum E
{
    None = 0,
    A = 0
}
[|// Some comment
[System.Flags]
internal enum E2
{
    None = 0,
    A = None
}|]";

            var expectedFixedCode = @"// Some comment
[System.Flags]
private enum E
{
    None = 0,
    A = 0
}
// Some comment
[System.Flags]
internal enum E2
{
    None = 0
}";
            VerifyCSharpFix(code, expectedFixedCode);
        }

        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumsShouldZeroValueNotFlagsNoZeroValue()
        {
            var code = @"
private enum E
{
    A = 1
}

private enum E2
{
    None = 1,
    A = 2
}

internal enum E3
{
    None = 0,
    A = 1
}

internal enum E4
{
    None = 0,
    A = 0
}
";

            var expectedFixedCode = @"
private enum E
{
    None,
    A = 1
}

private enum E2
{
    None,
    A = 2
}

internal enum E3
{
    None = 0,
    A = 1
}

internal enum E4
{
    None = 0,
    A = 0
}
";
            VerifyCSharpFix(code, expectedFixedCode);
        }

        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CSharp_EnumsShouldZeroValueNotFlagsNoZeroValueWithScope()
        {
            var code = @"
class C
{
    private enum E
    {
        A = 1
    }

    [|private enum E2
    {
        None = 1,
        A = 2
    }

    internal enum E3
    {
        None = 0,
        A = 1
    }|]

    internal enum E4
    {
        None = 0,
        A = 0
    }
}
";

            var expectedFixedCode = @"
class C
{
    private enum E
    {
        A = 1
    }

    private enum E2
    {
        None,
        A = 2
    }

    internal enum E3
    {
        None = 0,
        A = 1
    }

    internal enum E4
    {
        None = 0,
        A = 0
    }
}
";

            VerifyCSharpFix(code, expectedFixedCode);
        }

        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumsShouldZeroValueFlagsRename()
        {
            var code = @"
<System.Flags>
Private Enum E
	A = 0
	B = 1
End Enum

<System.Flags>
Public Enum E2
	A2 = 0
	B2 = 1
End Enum

<System.Flags>
Public Enum E3
	A3 = CUShort(0)
	B3 = CUShort(1)
End Enum

<System.Flags>
Public Enum NoZeroValuedField
    A5 = 1
    B5 = 2
End Enum
";

            var expectedFixedCode = @"
<System.Flags>
Private Enum E
    None
    B = 1
End Enum

<System.Flags>
Public Enum E2
    None
    B2 = 1
End Enum

<System.Flags>
Public Enum E3
    None
    B3 = CUShort(1)
End Enum

<System.Flags>
Public Enum NoZeroValuedField
    A5 = 1
    B5 = 2
End Enum
";
            VerifyBasicFix(code, expectedFixedCode);
        }

        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumsShouldZeroValueFlagsRenameScope()
        {
            var code = @"
<System.Flags>
Private Enum E
	A = 0
	B = 1
End Enum

[|<System.Flags>
Public Enum E2
	A2 = 0
	B2 = 1
End Enum

<System.Flags>
Public Enum E3
	A3 = CUShort(0)
	B3 = CUShort(1)
End Enum|]

<System.Flags>
Public Enum NoZeroValuedField
    A5 = 1
    B5 = 2
End Enum
";

            var expectedFixedCode = @"
<System.Flags>
Private Enum E
	A = 0
	B = 1
End Enum

<System.Flags>
Public Enum E2
    None
    B2 = 1
End Enum

<System.Flags>
Public Enum E3
    None
    B3 = CUShort(1)
End Enum

<System.Flags>
Public Enum NoZeroValuedField
    A5 = 1
    B5 = 2
End Enum
";
            VerifyBasicFix(code, expectedFixedCode);
        }

        [WorkItem(836193)]
        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumsShouldZeroValueFlagsRename_AttributeListHasTrivia()
        {
            var code = @"
<System.Flags> _
Private Enum E
	A = 0
	B = 1
End Enum

<System.Flags> _
Public Enum E2
	A2 = 0
	B2 = 1
End Enum

<System.Flags> _
Public Enum E3
	A3 = CUShort(0)
	B3 = CUShort(1)
End Enum

<System.Flags> _
Public Enum NoZeroValuedField
	A5 = 1
	B5 = 2
End Enum
";

            var expectedFixedCode = @"
<System.Flags> _
Private Enum E
    None
    B = 1
End Enum

<System.Flags> _
Public Enum E2
    None
    B2 = 1
End Enum

<System.Flags> _
Public Enum E3
    None
    B3 = CUShort(1)
End Enum

<System.Flags> _
Public Enum NoZeroValuedField
	A5 = 1
	B5 = 2
End Enum
";
            VerifyBasicFix(code, expectedFixedCode);
        }

        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumsShouldZeroValueFlagsMultipleZero()
        {
            var code = @"
<System.Flags>
Private Enum E
	None = 0
	A = 0
End Enum

<System.Flags>
Friend Enum E2
	None = 0
	A = None
End Enum

<System.Flags>
Public Enum E3
	A3 = 0
	B3 = CUInt(0)  ' Not a constant
End Enum";

            var expectedFixedCode = @"
<System.Flags>
Private Enum E
    None = 0
End Enum

<System.Flags>
Friend Enum E2
    None = 0
End Enum

<System.Flags>
Public Enum E3
    None
End Enum";

            VerifyBasicFix(code, expectedFixedCode);
        }

        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumsShouldZeroValueNotFlagsNoZeroValue()
        {
            var code = @"
Private Enum E
	A = 1
End Enum

Private Enum E2
	None = 1
	A = 2
End Enum

Friend Enum E3
    None = 0
    A = 1
End Enum

Friend Enum E4
    None = 0
    A = 0
End Enum
";

            var expectedFixedCode = @"
Private Enum E
    None
    A = 1
End Enum

Private Enum E2
    None
    A = 2
End Enum

Friend Enum E3
    None = 0
    A = 1
End Enum

Friend Enum E4
    None = 0
    A = 0
End Enum
";
            VerifyBasicFix(code, expectedFixedCode);
        }

        [Fact(Skip = "Bug 880044"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void VisualBasic_EnumsShouldZeroValueNotFlagsNoZeroValueWithScope()
        {
            var code = @"
Private Enum E
	A = 1
End Enum

[|Private Enum E2
	None = 1
	A = 2
End Enum

Friend Enum E3
    None = 0
    A = 1
End Enum|]

Friend Enum E4
    None = 0
    A = 0
End Enum
";

            var expectedFixedCode = @"
Private Enum E
	A = 1
End Enum

Private Enum E2
    None
    A = 2
End Enum

Friend Enum E3
    None = 0
    A = 1
End Enum

Friend Enum E4
    None = 0
    A = 0
End Enum
";
            VerifyBasicFix(code, expectedFixedCode);
        }
    }
}
