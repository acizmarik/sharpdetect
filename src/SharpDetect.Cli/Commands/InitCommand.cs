// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using SharpDetect.Cli.Handlers;

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

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new InitCommandHandler(OutputFile, PluginType, TargetPath);
        await handler.ExecuteAsync(console, CancellationToken.None);
    }
}

