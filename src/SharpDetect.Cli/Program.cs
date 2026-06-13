// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Cli;

public static class Program
{
    private const string ProgramTitle = "SharpDetect";
    private const string ProgramDescription = "Dynamic analysis framework for .NET programs";
    private const string ExecutableName = "sharpdetect";
    
    public static async Task<int> Main()
    {
        EnvironmentUtils.Initialize();
        var builder = new CommandLineApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .SetTitle(ProgramTitle)
            .SetDescription(ProgramDescription)
            .SetExecutableName(ExecutableName);

        return await builder.Build().RunAsync();
    }
}
