// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Output;

internal sealed record OutputOptions(Verbosity? Verbosity = null, string? ReportPath = null, bool NoReport = false)
{
    private const string ReportFileExtension = ".html";

    public static readonly OutputOptions Default = new();

    public OutputOptions MapReportPath(Func<string, string> map)
        => ReportPath is null ? this : this with { ReportPath = map(ReportPath) };
    
    public RunCommandArgs ApplyTo(RunCommandArgs arguments)
    {
        var analysis = arguments.Analysis;
        var (reportsFolder, reportFileName) = ReportPath is null
            ? (analysis.ReportsFolder, analysis.ReportFileName)
            : SplitReportPath(ReportPath);

        var profiler = arguments.Runtime.Profiler;
        return arguments with
        {
            Runtime = Verbosity is not { } verbosity
                ? arguments.Runtime
                : arguments.Runtime with
                {
                    Profiler = new ProfilerConfigurationArgs(
                        pathWindowsX64Raw: profiler.PathWindowsX64Raw,
                        pathLinuxX64Raw: profiler.PathLinuxX64Raw,
                        clsidRaw: profiler.ClsidRaw,
                        logLevel: verbosity.ToProfilerLogLevel())
                },
            Analysis = new AnalysisPluginConfigurationArgs(
                configuration: analysis.Configuration,
                pluginFullTypeName: analysis.PluginFullTypeName,
                pluginName: analysis.PluginName,
                pathRaw: analysis.PathRaw,
                renderReport: !NoReport && (ReportPath is not null || analysis.RenderReport),
                logLevel: Verbosity?.ToAnalysisLogLevel() ?? analysis.LogLevel,
                temporaryFilesFolder: analysis.TemporaryFilesFolder,
                reportsFolder: reportsFolder,
                reportFileName: reportFileName,
                sessionId: analysis.SessionId)
        };
    }
    
    private static (string? Folder, string? FileName) SplitReportPath(string reportPath)
    {
        if (!reportPath.EndsWith(ReportFileExtension, StringComparison.OrdinalIgnoreCase))
            return (reportPath, null);

        var folder = Path.GetDirectoryName(reportPath);
        return (string.IsNullOrEmpty(folder) ? "." : folder, Path.GetFileName(reportPath));
    }
}
