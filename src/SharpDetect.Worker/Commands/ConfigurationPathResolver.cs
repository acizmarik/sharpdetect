// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Worker.Commands;

public static class ConfigurationPathResolver
{
    public static RunCommandArgs Resolve(RunCommandArgs arguments, string baseDirectory)
    {
        baseDirectory = Path.GetFullPath(baseDirectory);
        return arguments with
        {
            Runtime = Resolve(arguments.Runtime, baseDirectory),
            Target = Resolve(arguments.Target, baseDirectory),
            Analysis = Resolve(arguments.Analysis, baseDirectory)
        };
    }
    
    public static string MakeRelative(string path, string currentDirectory, string configurationDirectory)
    {
        if (path.StartsWith('%'))
            return path;

        var absolutePath = Path.GetFullPath(path, currentDirectory);
        var relativePath = Path.GetRelativePath(Path.GetFullPath(configurationDirectory), absolutePath);
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static RuntimeConfigurationArgs Resolve(RuntimeConfigurationArgs runtime, string baseDirectory)
    {
        var profiler = runtime.Profiler;
        var host = runtime.Host;

        return runtime with
        {
            Profiler = new ProfilerConfigurationArgs(
                pathWindowsX64Raw: ResolveOptional(profiler.PathWindowsX64Raw is null ? null : profiler.PathWindowsX64, baseDirectory),
                pathLinuxX64Raw: ResolveOptional(profiler.PathLinuxX64Raw is null ? null : profiler.PathLinuxX64, baseDirectory),
                clsidRaw: profiler.ClsidRaw,
                logLevel: profiler.LogLevel),
            Host = host is null
                ? null
                : new HostConfigurationArgs(
                    path: IsCommandName(host.Path) ? host.Path : ResolvePath(host.Path, baseDirectory),
                    args: host.Args,
                    additionalEnvironmentVariables: host.AdditionalEnvironmentVariables)
        };
    }

    private static TargetConfigurationArgs Resolve(TargetConfigurationArgs target, string baseDirectory)
    {
        var redirects = target.RedirectInputOutput;
        return new TargetConfigurationArgs(
            path: ResolvePath(target.Path, baseDirectory),
            args: target.Args,
            workingDirectory: ResolveOptional(target.WorkingDirectory, baseDirectory),
            additionalEnvironmentVariables: target.AdditionalEnvironmentVariables,
            redirectInputOutput: redirects is null
                ? null
                : new RedirectInputOutputConfigurationArgs(
                    singleConsoleMode: redirects.SingleConsoleMode,
                    stdinFilePath: ResolveOptional(redirects.StdinFilePath, baseDirectory),
                    stdoutFilePath: ResolveOptional(redirects.StdoutFilePath, baseDirectory),
                    stderrFilePath: ResolveOptional(redirects.StderrFilePath, baseDirectory)),
            kind: target.Kind,
            test: target.Test);
    }

    private static AnalysisPluginConfigurationArgs Resolve(AnalysisPluginConfigurationArgs analysis, string baseDirectory)
    {
        return new AnalysisPluginConfigurationArgs(
            configuration: analysis.Configuration,
            pluginFullTypeName: analysis.PluginFullTypeName,
            pluginName: analysis.PluginName,
            pathRaw: ResolveOptional(analysis.PathRaw is null ? null : analysis.Path, baseDirectory),
            renderReport: analysis.RenderReport,
            logLevel: analysis.LogLevel,
            temporaryFilesFolder: ResolveOptional(analysis.TemporaryFilesFolder, baseDirectory),
            reportsFolder: ResolveOptional(analysis.ReportsFolder, baseDirectory),
            reportFileName: analysis.ReportFileName,
            sessionId: analysis.SessionId);
    }

    private static bool IsCommandName(string path)
        => !path.Contains('/') && !path.Contains(Path.DirectorySeparatorChar);

    private static string? ResolveOptional(string? path, string baseDirectory)
        => string.IsNullOrEmpty(path) ? path : ResolvePath(path, baseDirectory);

    private static string ResolvePath(string path, string baseDirectory)
        => Path.GetFullPath(path, baseDirectory);
}
