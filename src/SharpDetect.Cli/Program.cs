// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
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
        var builder = new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .SetTitle(ProgramTitle)
            .SetDescription(ProgramDescription)
            .SetExecutableName(ExecutableName);
        DisableDebugModeInRelease(builder);
        
        return await builder.Build().RunAsync();
    }
    
    [Conditional("RELEASE")]
    private static void DisableDebugModeInRelease(CliApplicationBuilder builder)
    {
        builder.AllowDebugMode(isAllowed: false);
        builder.AllowPreviewMode(isAllowed: false);
    }
}
