// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx.Infrastructure;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class ExitCodeTests
{
    [Theory]
    [InlineData(ExitCode.Success, 0)]
    [InlineData(ExitCode.UsageError, 1)]
    [InlineData(ExitCode.AnalysisError, 2)]
    [InlineData(ExitCode.ConfigurationError, 3)]
    [InlineData(ExitCode.Cancelled, 4)]
    [InlineData(ExitCode.IssuesFound, 5)]
    [InlineData(ExitCode.AnalysisIncomplete, 6)]
    public void ExitCode_ValuesAreStable(ExitCode exitCode, int expected)
    {
        Assert.Equal(expected, (int)exitCode);
    }

    [Theory]
    [InlineData("run", "--nonexisting")]
    [InlineData("run", "--test", "--runner", "RandomNonExisting", "--plugin", "FastTrack", "--target", "tests.dll")]
    public async Task CommandLineParseError_ExitsWithUsageError(params string[] args)
    {
        using var console = new FakeInMemoryConsole();
        var application = Program.CreateApplicationBuilder().UseConsole(console).Build();

        var exitCode = await application.RunAsync(args);

        Assert.Equal((int)ExitCode.UsageError, exitCode);
    }
}
