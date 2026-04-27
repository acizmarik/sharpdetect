// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using SharpDetect.Cli.Handlers;

namespace SharpDetect.Cli.Commands;

[Command("run", Description = "Executes analysis based on provided configuration file")]
public sealed class RunCommand : ICommand
{
    [CommandParameter(0, Description = "Arguments file")]
    public required string ArgumentsFile { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var previousDirectory = Directory.GetCurrentDirectory();
        var directoryName = Path.GetDirectoryName(ArgumentsFile);
        var workingDirectory = string.IsNullOrEmpty(directoryName) ? previousDirectory : directoryName;
        RunCommandHandler? commandHandler = null;

        try
        {
            Directory.SetCurrentDirectory(workingDirectory);
            var fileName = Path.GetFileName(ArgumentsFile);
            commandHandler = new RunCommandHandler(fileName);
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
}
