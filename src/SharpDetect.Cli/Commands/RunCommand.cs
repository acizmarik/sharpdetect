// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using SharpDetect.Cli.Handlers;

namespace SharpDetect.Cli.Commands;

[Command("run", Description = "Executes analysis based on provided arguments file")]
public sealed class RunCommand : ICommand
{
    [CommandParameter(0, Description = "Arguments file")]
    public required string ArgumentsFile { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var previousDirectory = Directory.GetCurrentDirectory();
        var workingDirectory = Path.GetDirectoryName(ArgumentsFile)
            ?? throw new DirectoryNotFoundException($"Cannot find parent directory of file \"{ArgumentsFile}\".");
        RunCommandHandler? commandHandler = null;

        try
        {
            Directory.SetCurrentDirectory(workingDirectory);
            var fileName = Path.GetFileName(ArgumentsFile);
            commandHandler = new RunCommandHandler(fileName);
            await commandHandler.ExecuteAsync(console, CancellationToken.None);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
            commandHandler?.Dispose();
        }
    }
}
