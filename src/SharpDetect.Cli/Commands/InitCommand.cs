// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using SharpDetect.Cli.Handlers;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Commands;

[Command("init", Description = "Creates a configuration file for analysis")]
public sealed partial class InitCommand : ICommand
{
    [CommandOption("plugin", 'p', Description = "Plugin type full name")]
    public required string PluginType { get; set; }

    [CommandOption("target", 't', Description = "Target application path")]
    public required string TargetPath { get; set; }

    [CommandOption("output", 'o', Description = "Output file path for the configuration file")]
    private string OutputFile { get; set; } = "AnalysisDescriptor.json";
    
    [CommandOption("test", Description = "Treat the target as a test assembly")]
    private bool IsTest { get; set; }

    [CommandOption("filter", 'f', Description = "Test filter expression (requires --test)")]
    private string? TestFilter { get; set; }

    [CommandOption("runner", Description = "Test runner: Mtp (default) | VSTest (requires --test)")]
    private TestRunner TestRunner { get; set; } = TestTargetConfigurationArgs.DefaultRunner;

    [CommandOption("instrument-system-libraries", Description = "Instrument system assemblies (System, Microsoft, test frameworks..)")]
    private bool InstrumentSystemLibraries { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!IsTest && TestFilter is not null)
        {
            throw new CommandException(
                "--filter requires --test.",
                (int)ExitCode.ConfigurationError);
        }

        if (!IsTest && TestRunner != TestTargetConfigurationArgs.DefaultRunner)
        {
            throw new CommandException(
                "--runner requires --test.",
                (int)ExitCode.ConfigurationError);
        }

        try
        {
            var handler = new InitCommandHandler(
                outputFile: OutputFile,
                pluginNameOrTypeFullName: PluginType,
                targetAssemblyPath: TargetPath,
                isTest: IsTest,
                testRunner: TestRunner,
                testFilter: TestFilter,
                instrumentSystemLibraries: InstrumentSystemLibraries);
            var cancellationToken = console.RegisterCancellationHandler();
            await handler.ExecuteAsync(console, cancellationToken);
        }
        catch (CommandException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CommandException(
                message: exception.Message,
                exitCode: (int)ExitCode.ConfigurationError,
                innerException: exception);
        }
    }
}
