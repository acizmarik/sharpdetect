// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx.Infrastructure;
using SharpDetect.Plugins.DataRace.FastTrack;
using SharpDetect.Worker.Configuration;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class PluginsCommandTests
{
    [Fact]
    public async Task Plugins_ListsShippedPlugins()
    {
        EnvironmentUtils.Initialize();
        using var console = new FakeInMemoryConsole();
        var application = Program.CreateApplicationBuilder().UseConsole(console).Build();

        var exitCode = await application.RunAsync(["plugins"]);

        Assert.Equal(0, exitCode);
        var output = console.ReadOutputString();
        Assert.Contains("FastTrack", output);
        Assert.Contains("Vector-clock data race detector.", output);
        Assert.Contains(typeof(FastTrackPlugin).FullName!, output);
        Assert.Contains("Deadlock", output);
    }

    [Fact]
    public async Task Plugins_WithMissingAssembly_ExitsWithConfigurationError()
    {
        using var console = new FakeInMemoryConsole();
        var application = Program.CreateApplicationBuilder().UseConsole(console).Build();

        var exitCode = await application.RunAsync(["plugins", "--plugin-assembly", "does-not-exist.dll"]);

        Assert.Equal((int)ExitCode.ConfigurationError, exitCode);
        Assert.Contains("does-not-exist.dll", console.ReadErrorString());
    }
}
