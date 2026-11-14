// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using SharpDetect.Cli.Handlers;

namespace SharpDetect.Cli.Commands;

[Command("init", Description = "Creates a configuration file for analysis")]
public sealed partial class InitCommand(PassThroughArguments passThroughArguments)
    : AnalysisCommandBase(passThroughArguments)
{
    [CommandOption("output", 'o', Description = "Output file path for the configuration file")]
    public string OutputFile { get; set; } = "AnalysisDescriptor.json";

    public override async ValueTask ExecuteAsync(IConsole console)
    {
        ValidateSharedOptions();
        var options = ToAnalysisOptions();

        try
        {
            var handler = new InitCommandHandler(OutputFile, options);
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
                message: ExceptionMessages.Flatten(exception),
                exitCode: (int)ExitCode.ConfigurationError,
                innerException: exception);
        }
    }
}
