// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx.Infrastructure;
using SharpDetect.Cli.Handlers;
using SharpDetect.Worker;
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
    [InlineData(false, false, ExitCode.Success)]
    [InlineData(false, true, ExitCode.IssuesFound)]
    [InlineData(true, false, ExitCode.AnalysisIncomplete)]
    [InlineData(true, true, ExitCode.AnalysisIncomplete)]
    public void GetExitCode_IncompleteAnalysisTakesPrecedenceOverIssues(bool incomplete, bool issuesFound, ExitCode expected)
    {
        AnalysisGap[] gaps = incomplete
            ? [new AnalysisGap(AnalysisGapKind.ProcessNotFinished, "Process 42 did not report the end of its events.")]
            : [];

        var exitCode = RunCommandHandler.GetExitCode(new AnalysisOutcome(gaps), issuesFound);

        Assert.Equal(expected, exitCode);
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
