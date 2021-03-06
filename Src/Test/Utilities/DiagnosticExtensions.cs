﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis
{
    public static class DiagnosticExtensions
    {
        private const int EN_US = 1033;
        public static Func<Exception, IDiagnosticAnalyzer, bool> AlwaysCatchAnalyzerExceptions = (e, a) => true;
        public static Func<Exception, IDiagnosticAnalyzer, bool> DonotCatchAnalyzerExceptions = (e, a) => e is OperationCanceledException;

        /// <summary>
        /// This is obsolete. Use Verify instead.
        /// </summary>
        public static void VerifyErrorCodes(this IEnumerable<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            Verify(actual, expected, errorCodeOnly: true);
        }

        public static void VerifyErrorCodes(this ImmutableArray<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            VerifyErrorCodes((IEnumerable<Diagnostic>)actual, expected);
        }

        internal static void Verify(this DiagnosticBag actual, params DiagnosticDescription[] expected)
        {
            Verify(actual.AsEnumerable(), expected, errorCodeOnly: false);
        }

        public static void Verify(this IEnumerable<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            Verify(actual, expected, errorCodeOnly: false);
        }

        public static void Verify(this ImmutableArray<Diagnostic> actual, params DiagnosticDescription[] expected)
        {
            if (CultureInfo.CurrentCulture.LCID == EN_US || CultureInfo.CurrentUICulture.LCID == EN_US || CultureInfo.CurrentCulture == CultureInfo.InvariantCulture || CultureInfo.CurrentUICulture == CultureInfo.InvariantCulture)
            {
                Verify((IEnumerable<Diagnostic>)actual, expected);
            }
            else
            {
                actual.VerifyErrorCodes(expected);
            }
        }

        private static void Verify(IEnumerable<Diagnostic> actual, DiagnosticDescription[] expected, bool errorCodeOnly)
        {
            if (expected == null)
            {
                throw new ArgumentException("Must specify expected errors.", "expected");
            }

            var unmatched = actual.Select(d => new DiagnosticDescription(d, errorCodeOnly)).ToList();

            // Try to match each of the 'expected' errors to one of the 'actual' ones.
            // If any of the expected errors don't appear, fail test.
            foreach (var d in expected)
            {
                int index = unmatched.IndexOf(d);
                if (index > -1)
                {
                    unmatched.RemoveAt(index);
                }
                else
                {
                    Assert.True(false, DiagnosticDescription.GetAssertText(expected, actual));
                }
            }

            // If any 'extra' errors appear that were not in the 'expected' list, fail test.
            if (unmatched.Count > 0)
            {
                Assert.True(false, DiagnosticDescription.GetAssertText(expected, actual));
            }
        }

        public static TCompilation VerifyDiagnostics<TCompilation>(this TCompilation c, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            var diagnostics = c.GetDiagnostics();
            diagnostics.Verify(expected);
            return c;
        }

        public static CSharpCompilation VerifyCSharpAnalyzerDiagnostics(this CSharpCompilation c, IDiagnosticAnalyzer[] analyzers, AnalyzerOptions options = null, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null, params DiagnosticDescription[] expected)
        {
            return VerifyAnalyzerDiagnostics(c, n => n.CSharpKind(), analyzers, options, expected, continueOnAnalyzerException);
        }

        public static VisualBasicCompilation VerifyVisualBasicAnalyzerDiagnostics(this VisualBasicCompilation c, IDiagnosticAnalyzer[] analyzers, AnalyzerOptions options = null, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null, params DiagnosticDescription[] expected)
        {
            return VerifyAnalyzerDiagnostics(c, n => n.VisualBasicKind(), analyzers, options, expected, continueOnAnalyzerException);
        }

        public static TCompilation VerifyAnalyzerOccuranceCount<TCompilation>(this TCompilation c, IDiagnosticAnalyzer[] analyzers, int expectedCount, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null)
            where TCompilation : Compilation
        {
            var csComp = c as CSharpCompilation;
            if (csComp != null)
            {
                Assert.Equal(expectedCount, csComp.GetCSharpAnalyzerDiagnostics(analyzers, null, continueOnAnalyzerException).Length);
                return c;
            }
            else
            {
                var vbComp = c as VisualBasicCompilation;
                Assert.Equal(expectedCount, vbComp.GetVisualBasicAnalyzerDiagnostics(analyzers, null, continueOnAnalyzerException).Length);
                return c;
            }
        }

        public static TCompilation VerifyAnalyzerDiagnostics<TCompilation>(
                this TCompilation c, IDiagnosticAnalyzer[] analyzers, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            return c.VerifyAnalyzerDiagnostics(analyzers, null, continueOnAnalyzerException, expected);
        }

        public static TCompilation VerifyAnalyzerDiagnostics<TCompilation>(
                this TCompilation c, IDiagnosticAnalyzer[] analyzers, AnalyzerOptions options, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            var csComp = c as CSharpCompilation;
            if (csComp != null)
            {
                return csComp.VerifyCSharpAnalyzerDiagnostics(analyzers, options, continueOnAnalyzerException, expected) as TCompilation;
            }
            else
            {
                var vbComp = c as VisualBasicCompilation;
                return vbComp.VerifyVisualBasicAnalyzerDiagnostics(analyzers, options, continueOnAnalyzerException, expected) as TCompilation;
            }
        }

        private static TCompilation VerifyAnalyzerDiagnostics<TCompilation, TSyntaxKind>(
                this TCompilation c, Func<SyntaxNode, TSyntaxKind> getKind, IDiagnosticAnalyzer[] analyzers, AnalyzerOptions options, DiagnosticDescription[] expected, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null)
            where TCompilation : Compilation
            where TSyntaxKind : struct
        {
            ImmutableArray<Diagnostic> diagnostics;
            c = c.GetAnalyzerDiagnostics(getKind, analyzers, options, continueOnAnalyzerException, diagnostics: out diagnostics);
            diagnostics.Verify(expected);
            return c; // note this is a new compilation
        }

        public static ImmutableArray<Diagnostic> GetCSharpAnalyzerDiagnostics(this CSharpCompilation c, IDiagnosticAnalyzer[] analyzers, AnalyzerOptions options = null, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null)
        {
            ImmutableArray<Diagnostic> diagnostics;
            c = GetAnalyzerDiagnostics(c, n => n.CSharpKind(), analyzers, options, continueOnAnalyzerException, out diagnostics);
            return diagnostics;
        }

        public static ImmutableArray<Diagnostic> GetVisualBasicAnalyzerDiagnostics(this VisualBasicCompilation c, IDiagnosticAnalyzer[] analyzers, AnalyzerOptions options = null, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null)
        {
            ImmutableArray<Diagnostic> diagnostics;
            c = GetAnalyzerDiagnostics(c, n => n.VisualBasicKind(), analyzers, options, continueOnAnalyzerException, out diagnostics);
            return diagnostics;
        }

        public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics<TCompilation>(this TCompilation c, IDiagnosticAnalyzer[] analyzers, AnalyzerOptions options = null, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null)
            where TCompilation : Compilation
        {
            var csComp = c as CSharpCompilation;
            if (csComp != null)
            {
                return csComp.GetCSharpAnalyzerDiagnostics(analyzers, options, continueOnAnalyzerException);
            }
            else
            {
                var vbComp = c as VisualBasicCompilation;
                return vbComp.GetVisualBasicAnalyzerDiagnostics(analyzers, options, continueOnAnalyzerException);
            }
        }

        private static TCompilation GetAnalyzerDiagnostics<TCompilation, TSyntaxKind>(
                this TCompilation c,
                Func<SyntaxNode, TSyntaxKind> getKind,
                IDiagnosticAnalyzer[] analyzers,
                AnalyzerOptions options,
                Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException,
                out ImmutableArray<Diagnostic> diagnostics)
            where TCompilation : Compilation
            where TSyntaxKind : struct
        {
            // We want unit tests to throw if any analyzer OR the driver throws, unless the test explicitly provides a delegate.
            continueOnAnalyzerException = continueOnAnalyzerException ?? DonotCatchAnalyzerExceptions;

            var driver = new AnalyzerDriver<TSyntaxKind>(analyzers.ToImmutableArray(), getKind, options, CancellationToken.None, continueOnAnalyzerException);
            c = (TCompilation)c.WithEventQueue(driver.CompilationEventQueue);
            var discarded = c.GetDiagnostics();
            diagnostics = driver.GetDiagnosticsAsync().Result;
            return c; // note this is a new compilation
        }

        /// <summary>
        /// Given a set of compiler or <see cref="IDiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
        /// 1) <see cref="CompilationOptions.SpecificDiagnosticOptions"/> specified for the given <paramref name="compilation"/>.
        /// 2) <see cref="CompilationOptions.GeneralDiagnosticOption"/> specified for the given <paramref name="compilation"/>.
        /// 3) Diagnostic suppression through applied <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
        /// 4) Pragma directives for the given <paramref name="compilation"/>.
        /// </summary>
        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(this Compilation compilation, IEnumerable<Diagnostic> diagnostics)
        {
            return AnalyzerDriver.GetEffectiveDiagnostics(diagnostics, compilation);
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// <paramref name="continueOnError"/> says whether the caller would like the exception thrown by the analyzers to be handled or not. If true - Handles ; False - Not handled.
        /// </summary>
        public static bool IsDiagnosticAnalyzerSuppressed(this IDiagnosticAnalyzer analyzer, CompilationOptions options)
        {
            return AnalyzerDriver.IsDiagnosticAnalyzerSuppressed(analyzer, options, (exception, throwingAnalyzer) => true);
        }

        public static TCompilation VerifyEmitDiagnostics<TCompilation>(this TCompilation c, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            c.Emit(new MemoryStream(), pdbStream: new MemoryStream()).Diagnostics.Verify(expected);
            return c;
        }

        public static TCompilation VerifyEmitDiagnostics<TCompilation>(this TCompilation c, IEnumerable<ResourceDescription> manifestResources, params DiagnosticDescription[] expected)
            where TCompilation : Compilation
        {
            c.Emit(new MemoryStream(), pdbStream: new MemoryStream(), manifestResources: manifestResources).Diagnostics.Verify(expected);
            return c;
        }

        public static string Concat(this string[] str)
        {
            return str.Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine(s), sb => sb.ToString());
        }
    }
}
