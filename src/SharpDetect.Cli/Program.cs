// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using CliFx;
using CliFx.Infrastructure;
using SharpDetect.Cli.Commands;
using SharpDetect.Core.Reporting;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Cli;

public static class Program
{
    private const string ProgramTitle = "SharpDetect";
    private const string ExecutableName = "sharpdetect";
    
    public static async Task<int> Main(string[] args)
    {
        EnvironmentUtils.Initialize();
        return await RunAsync(args);
    }

    internal static async Task<int> RunAsync(IReadOnlyList<string> commandLine, IConsole? console = null)
    {
        var (ownArguments, passThroughArguments) = PassThroughArguments.Split(commandLine);
        var builder = CreateApplicationBuilder(passThroughArguments);
        if (console is not null)
            builder.UseConsole(console);

        return await builder.Build().RunAsync(ownArguments);
    }

    internal static CommandLineApplicationBuilder CreateApplicationBuilder(PassThroughArguments? passThroughArguments = null)
    {
        passThroughArguments ??= PassThroughArguments.None;
        return new CommandLineApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .UseTypeInstantiator(type => CreateCommand(type, passThroughArguments))
            .SetTitle(ProgramTitle)
            .SetVersion(SharpDetectVersion.Current)
            .SetDescription(typeof(Program).Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description)
            .SetExecutableName(ExecutableName);
    }

    private static object CreateCommand(Type type, PassThroughArguments passThroughArguments)
    {
        if (type == typeof(RunCommand))
            return new RunCommand(passThroughArguments);
        if (type == typeof(InitCommand))
            return new InitCommand(passThroughArguments);

        return Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException($"Could not create an instance of {type}.");
    }
}
