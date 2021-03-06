﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageServiceFactory(typeof(IProjectFileLoader), LanguageNames.CSharp)]
    [ProjectFileExtension("csproj")]
    [ProjectTypeGuid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")]
    internal class CSharpProjectFileLoaderFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpProjectFileLoader(languageServices.WorkspaceServices);
        }
    }
}