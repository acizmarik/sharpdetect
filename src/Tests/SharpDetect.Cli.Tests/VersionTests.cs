// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using CliFx.Infrastructure;
using SharpDetect.Core.Reporting;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class VersionTests
{
    [Fact]
    public async Task Version_PrintsInformationalVersion()
    {
        var informationalVersion = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;
        using var console = new FakeInMemoryConsole();

        var exitCode = await Program.RunAsync(["--version"], console);

        Assert.Equal(0, exitCode);
        var printed = console.ReadOutputString().Trim();
        Assert.Equal(SharpDetectVersion.Current, printed);
        Assert.StartsWith(SharpDetectVersion.Format(informationalVersion), printed);
        var packageVersion = informationalVersion.Split('+')[0];
        Assert.StartsWith(packageVersion, printed);
        if (packageVersion.Contains('-'))
            Assert.Contains(packageVersion[packageVersion.IndexOf('-')..], printed);
    }

    [Fact]
    public async Task Help_BannerShowsInformationalVersion()
    {
        using var console = new FakeInMemoryConsole();

        await Program.RunAsync(["--help"], console);

        Assert.Contains($"SharpDetect {SharpDetectVersion.Current}", console.ReadOutputString());
    }
}
