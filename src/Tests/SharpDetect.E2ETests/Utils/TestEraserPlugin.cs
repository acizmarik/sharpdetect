// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Configuration;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.Core.Serialization;
using SharpDetect.Plugins.DataRace.Eraser;

namespace SharpDetect.E2ETests.Utils;

public sealed class TestEraserPlugin : EraserPlugin
{
    public TestEraserPlugin(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        IArgumentsParser argumentsParser,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        PathsConfiguration pathsConfiguration,
        TimeProvider timeProvider,
        ILogger<EraserPlugin> logger)
        : base(
            moduleBindContext,
            metadataContext,
            argumentsParser,
            profilerCommandSenderProvider,
            pathsConfiguration,
            timeProvider,
            logger)
    {
    }
    
    public IEnumerable<Report> GetSubjectReports()
    {
        var allReports = CreateDiagnostics().GetAllReports();
        return allReports.Where(IsSubjectReport);
    }

    private static bool IsSubjectReport(Report report)
    {
        var description = report.Description;
        if (description.Contains("SharpDetect.E2ETests.Subject"))
            return true;

        return !(description.Contains("System.") || 
                 description.Contains("Microsoft.") || 
                 description.Contains("Internal."));
    }
}
