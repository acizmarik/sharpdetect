// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using SharpDetect.Cli.Handlers;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Commands;

[Command("init", Description = "Creates a configuration file for analysis")]
public sealed class InitCommand : ICommand
{
    [CommandOption("output", 'o', Description = "Output file path for the configuration file")]
    public string OutputFile { get; init; } = "AnalysisDescriptor.json";

    [CommandOption("plugin", 'p', Description = "Plugin type full name")]
    public required string PluginType { get; init; }

    [CommandOption("target", 't', Description = "Target application path")]
    public required string TargetPath { get; init; }

    [CommandOption("test", Description = "Treat the target as a test assembly")]
    public bool IsTest { get; init; }

    [CommandOption("filter", 'f', Description = "Test filter expression (requires --test)")]
    public string? TestFilter { get; init; }

    [CommandOption("runner", Description = "Test runner: Mtp (default) | VSTest (requires --test)")]
    public TestRunner TestRunner { get; init; } = TestTargetConfigurationArgs.DefaultRunner;

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
                testFilter: TestFilter);
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
