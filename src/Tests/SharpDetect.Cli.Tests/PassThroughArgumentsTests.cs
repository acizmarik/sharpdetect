// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx.Infrastructure;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class PassThroughArgumentsTests
{
    [Fact]
    public void Split_WithoutSeparator_PassesNothingThrough()
    {
        var (own, passThrough) = PassThroughArguments.Split(["run", "-t", "app.dll"]);

        Assert.Equal(new[] { "run", "-t", "app.dll" }, own);
        Assert.False(passThrough.IsSpecified);
        Assert.Empty(passThrough.Arguments);
    }

    [Fact]
    public void Split_AtFirstSeparator_KeepsLaterSeparatorsForTarget()
    {
        var (own, passThrough) = PassThroughArguments.Split(["run", "-t", "app.dll", "--", "--help", "--", "x y"]);

        Assert.Equal(new[] { "run", "-t", "app.dll" }, own);
        Assert.True(passThrough.IsSpecified);
        Assert.Equal(new[] { "--help", "--", "x y" }, passThrough.Arguments);
    }

    [Fact]
    public void Split_TrailingSeparator_IsSpecifiedButEmpty()
    {
        var (_, passThrough) = PassThroughArguments.Split(["run", "-t", "app.dll", "--"]);

        Assert.True(passThrough.IsSpecified);
        Assert.Empty(passThrough.Arguments);
    }

    [Fact]
    public async Task Init_WritesPassThroughArgumentsAsTargetArgs()
    {
        var json = await InitAndRead(["-t", typeof(PassThroughArgumentsTests).Assembly.Location, "--", "race", "two words"]);

        Assert.Contains(
            """
                "Args": [
                  "race",
                  "two words"
                ],
            """.ReplaceLineEndings(),
            json.ReplaceLineEndings());
        Assert.DoesNotContain("AdditionalRunnerArgs", json);
    }

    [Fact]
    public async Task Init_WithTest_WritesPassThroughArgumentsAsRunnerArgs()
    {
        var json = await InitAndRead(["-t", typeof(PassThroughArgumentsTests).Assembly.Location, "--test", "--", "--report-trx"]);

        Assert.Contains("\"AdditionalRunnerArgs\": [", json);
        Assert.Contains("\"--report-trx\"", json);
        Assert.Contains("\"Args\": []", json);
    }

    [Fact]
    public async Task Run_WithConfigurationFileAndPassThroughArguments_IsRejected()
    {
        var configurationFile = Path.GetTempFileName();
        try
        {
            using var console = new FakeInMemoryConsole();

            var exitCode = await Program.RunAsync(["run", configurationFile, "--", "a"], console);

            Assert.Equal((int)ExitCode.ConfigurationError, exitCode);
            Assert.Contains("-- <arguments> cannot be combined with a configuration file", console.ReadErrorString());
        }
        finally
        {
            File.Delete(configurationFile);
        }
    }

    private static async Task<string> InitAndRead(string[] initArguments)
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var outputFile = Path.Combine(directory.FullName, "sd.json");
            using var console = new FakeInMemoryConsole();

            var exitCode = await Program.RunAsync(["init", "-o", outputFile, .. initArguments], console);

            Assert.True(exitCode == 0, console.ReadErrorString());
            return await File.ReadAllTextAsync(outputFile);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
