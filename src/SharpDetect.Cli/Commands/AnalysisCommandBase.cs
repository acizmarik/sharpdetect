// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Commands;

public abstract partial class AnalysisCommandBase : ICommand
{
    [CommandOption("plugin", 'p', Description = $"Analysis plugin name or plugin type full name. Default: {AnalysisOptions.DefaultPlugin}.")]
    public string? PluginType { get; set; }

    [CommandOption("target", 't', Description = "Target application or test assembly (.dll)")]
    public string? TargetPath { get; set; }

    [CommandOption("test", Description = "Treat the target as a test assembly")]
    public bool IsTest { get; set; }

    [CommandOption("filter", 'f', Description = "Test filter expression (requires --test)")]
    public string? TestFilter { get; set; }

    [CommandOption("runner", Description = "Test runner: Mtp (default) | VsTest (requires --test)")]
    public TestRunner? TestRunner { get; set; }

    [CommandOption("instrument-system-libraries", Description = "Instrument system assemblies (System, Microsoft, test frameworks..)")]
    public bool InstrumentSystemLibraries { get; set; }

    [CommandOption("stack-traces", Description = "Collect stack traces for racing accesses instead of a single frame")]
    public bool CollectStackTraces { get; set; }

    [CommandOption("stack-trace-depth", Description = "Maximum number of frames per collected stack trace (requires --stack-traces). Default: 8.")]
    public int? StackTraceDepth { get; set; }

    public abstract ValueTask ExecuteAsync(IConsole console);
    
    protected IEnumerable<string> GetSpecifiedInlineOptions()
    {
        if (!string.IsNullOrEmpty(PluginType))
            yield return "--plugin";
        if (!string.IsNullOrEmpty(TargetPath))
            yield return "--target";
        if (IsTest)
            yield return "--test";
        if (InstrumentSystemLibraries)
            yield return "--instrument-system-libraries";
        if (CollectStackTraces)
            yield return "--stack-traces";
    }

    protected void ValidateSharedOptions()
    {
        if (!IsTest && TestFilter is not null)
            throw UsageError("--filter requires --test.");

        if (!IsTest && TestRunner is not null)
            throw UsageError("--runner requires --test.");

        if (!CollectStackTraces && StackTraceDepth is not null)
            throw UsageError("--stack-trace-depth requires --stack-traces.");

        if (StackTraceDepth is < 1)
            throw UsageError("--stack-trace-depth must be at least 1.");
    }

    internal AnalysisOptions ToAnalysisOptions()
    {
        if (string.IsNullOrEmpty(TargetPath))
            throw UsageError("--target is required.");

        return new AnalysisOptions(
            PluginNameOrTypeFullName: string.IsNullOrEmpty(PluginType) ? AnalysisOptions.DefaultPlugin : PluginType,
            TargetPath: TargetPath,
            IsTest: IsTest,
            TestRunner: TestRunner,
            TestFilter: TestFilter,
            InstrumentSystemLibraries: InstrumentSystemLibraries,
            CollectStackTraces: CollectStackTraces,
            StackTraceDepth: StackTraceDepth);
    }

    protected static CommandException UsageError(string message)
        => new(message, (int)ExitCode.ConfigurationError, showHelp: true);
}
