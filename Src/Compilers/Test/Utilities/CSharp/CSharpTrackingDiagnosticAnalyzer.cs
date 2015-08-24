﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public class CSharpTrackingDiagnosticAnalyzer : TrackingDiagnosticAnalyzer<SyntaxKind>
    {
        static readonly Regex omittedSyntaxKindRegex =
            new Regex(@"Using|Extern|Parameter|Constraint|Specifier|Initializer|Global|Method|Destructor|MemberBindingExpression|ElementBindingExpression|ArrowExpressionClause|NameOfExpression|ConstantPattern|DeclarationPattern|WildCardPattern|RecursivePattern|PropertyPattern|ColonName|RecordDeclaration|MatchExpression|SubRecursivePattern|SubPropertyPattern|MatchStatement|CaseMatchLabel");
        
        protected override bool IsOnCodeBlockSupported(SymbolKind symbolKind, MethodKind methodKind, bool returnsVoid)
        {
            return base.IsOnCodeBlockSupported(symbolKind, methodKind, returnsVoid) && methodKind != MethodKind.EventRaise;
        }

        protected override bool IsAnalyzeNodeSupported(SyntaxKind syntaxKind)
        {
            return base.IsAnalyzeNodeSupported(syntaxKind) && !omittedSyntaxKindRegex.IsMatch(syntaxKind.ToString());
        }
    }
}
