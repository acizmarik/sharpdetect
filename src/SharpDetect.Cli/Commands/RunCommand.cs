// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Cli.Handlers;
using SharpDetect.Extensibility;
using SharpDetect.Reporting;

namespace SharpDetect.Cli.Commands
{
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
            RunCommandHandler commandHandler;

            try
            {
                Directory.SetCurrentDirectory(workingDirectory);
                commandHandler = new RunCommandHandler(ArgumentsFile);
                await commandHandler.ExecuteAsync(console);
            }
            finally
            {
                Directory.SetCurrentDirectory(previousDirectory);
            }

            if (commandHandler.Args.Analysis.RenderReport)
                ShowReportSummary(commandHandler.ServiceProvider);
        }

        private static void ShowReportSummary(IServiceProvider serviceProvider)
        {
            var displayer = serviceProvider.GetRequiredService<IReportSummaryDisplayer>();
            var summary = serviceProvider.GetRequiredService<PluginProxy>().GetReport();
            displayer.Display(summary);
        }
    }
}
