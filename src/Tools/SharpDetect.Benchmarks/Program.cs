// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Benchmarks;

public static class Program
{
    private const string ProgramTitle = "SharpDetect.Benchmarks";
    private const string ProgramDescription = "Performance baseline establishing harness for SharpDetect";
    private const string ExecutableName = "sharpdetect-benchmarks";

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
