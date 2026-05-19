// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using SharpDetect.Cli.Handlers;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Commands;

[Command("run", Description = "Executes analysis using inline options, or based on a provided configuration file")]
public sealed class RunCommand : ICommand
{
    [CommandParameter(0, IsRequired = false, Description = "Configuration file (omit to use inline options)")]
    public string? ArgumentsFile { get; init; }

    [CommandOption("plugin", 'p', Description = "Plugin type full name")]
    public string? PluginType { get; init; }

    [CommandOption("target", 't', Description = "Target application path")]
    public string? TargetPath { get; init; }

    [CommandOption("test", Description = "Treat the target as a test assembly")]
    public bool IsTest { get; init; }

    [CommandOption("filter", 'f', Description = "Test filter expression (requires --test)")]
    public string? TestFilter { get; init; }

    [CommandOption("runner", Description = "Test runner: Mtp (default) | VSTest (requires --test)")]
    public TestRunner TestRunner { get; init; } = TestTargetConfigurationArgs.DefaultRunner;

    [CommandOption("instrument-system-libraries", Description = "Instrument system assemblies (System, Microsoft, test frameworks..)")]
    public bool InstrumentSystemLibraries { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        ValidateOptions(ArgumentsFile, PluginType, TargetPath, IsTest, TestFilter, TestRunner, InstrumentSystemLibraries);

        var useInlineForm = string.IsNullOrEmpty(ArgumentsFile);
        var previousDirectory = Directory.GetCurrentDirectory();
        RunCommandHandler? commandHandler = null;

        try
        {
            if (useInlineForm)
            {
                var arguments = InitCommandHandler.BuildRunCommandArgs(
                    pluginNameOrTypeFullName: PluginType!,
                    targetAssemblyPath: TargetPath!,
                    isTest: IsTest,
                    testRunner: TestRunner,
                    testFilter: TestFilter,
                    instrumentSystemLibraries: InstrumentSystemLibraries);
                var rawJson = InitCommandHandler.SerializeRunCommandArgs(arguments);
                commandHandler = new RunCommandHandler(arguments, rawJson);
            }
            else
            {
                var directoryName = Path.GetDirectoryName(ArgumentsFile);
                var workingDirectory = string.IsNullOrEmpty(directoryName) ? previousDirectory : directoryName;
                Directory.SetCurrentDirectory(workingDirectory);
                var fileName = Path.GetFileName(ArgumentsFile!);
                commandHandler = new RunCommandHandler(fileName);
            }

            var cancellationToken = console.RegisterCancellationHandler();
            await commandHandler.ExecuteAsync(console, cancellationToken);
        }
        catch (CommandException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw new CommandException(
                message: "Analysis was cancelled by user",
                exitCode: (int)ExitCode.Cancelled);
        }
        catch (Exception exception)
        {
            throw new CommandException(
                message: "Analysis failed due to an internal error",
                exitCode: (int)ExitCode.AnalysisError,
                innerException: exception);
        }
        finally
        {
            commandHandler?.Dispose();
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    internal static void ValidateOptions(
        string? argumentsFile,
        string? pluginType,
        string? targetPath,
        bool isTest,
        string? testFilter,
        TestRunner testRunner,
        bool instrumentSystemLibraries = false)
    {
        var hasConfigFile = !string.IsNullOrEmpty(argumentsFile);
        var hasPlugin = !string.IsNullOrEmpty(pluginType);
        var hasTarget = !string.IsNullOrEmpty(targetPath);
        var hasInline = hasPlugin || hasTarget;

        if (hasConfigFile && hasInline)
            throw new CommandException(
                "Specify either a configuration file or inline options (--plugin and --target), not both.",
                (int)ExitCode.ConfigurationError);

        if (!hasConfigFile && !hasInline)
            throw new CommandException(
                "Either provide a configuration file or specify inline options (--plugin and --target).",
                (int)ExitCode.ConfigurationError);

        if (hasInline && (!hasPlugin || !hasTarget))
            throw new CommandException(
                "Both --plugin and --target are required when using inline options.",
                (int)ExitCode.ConfigurationError);

        if (!isTest && testFilter is not null)
            throw new CommandException(
                "--filter requires --test.",
                (int)ExitCode.ConfigurationError);

        if (!isTest && testRunner != TestTargetConfigurationArgs.DefaultRunner)
            throw new CommandException(
                "--runner requires --test.",
                (int)ExitCode.ConfigurationError);

        if (hasConfigFile && instrumentSystemLibraries)
            throw new CommandException(
                "--instrument-system-libraries cannot be combined with a configuration file; set SkipInstrumentationForAssemblies in the file instead.",
                (int)ExitCode.ConfigurationError);
    }
}
