// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;

namespace SharpDetect.Cli;

public static class Program
{
    public static async Task<int> Main()
    {
        var root = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        Environment.SetEnvironmentVariable("SHARPDETECT_ROOT", root);

        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .Build()
            .RunAsync();
    }
}
