// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class PlatformSupportTests
{
    [Fact]
    public void CurrentPlatform_IsSupported()
    {
        var platform = PlatformSupport.CheckCurrentPlatform();

        Assert.True(platform.IsSuccess);
        Assert.Equal(OperatingSystem.IsWindows() ? SupportedPlatform.WindowsX64 : SupportedPlatform.LinuxX64, platform.Value);
    }

    [Theory]
    [InlineData(SupportedPlatform.WindowsX64, "C:/profilers/win.dll")]
    [InlineData(SupportedPlatform.LinuxX64, "/profilers/linux.so")]
    public void ProfilerPath_IsSelectedByPlatform(SupportedPlatform platform, string expectedPath)
    {
        var profiler = new ProfilerConfigurationArgs(
            pathWindowsX64Raw: "C:/profilers/win.dll",
            pathLinuxX64Raw: "/profilers/linux.so");

        Assert.Equal(expectedPath, profiler.GetPath(platform));
    }

    [Theory]
    [InlineData("2.34")]
    [InlineData("2.39")]
    public void LinuxWithSupportedGlibc_IsSupported(string glibcVersion)
    {
        var status = PlatformSupport.CheckLinux(Version.Parse(glibcVersion));

        Assert.True(status.IsSuccess);
    }

    [Fact]
    public void LinuxWithoutGlibc_IsUnsupported()
    {
        var status = PlatformSupport.CheckLinux(glibcVersion: null);

        Assert.True(status.IsError);
        Assert.Equal("SharpDetect needs a Linux distribution with glibc 2.34 or newer.", status.Error);
    }

    [Fact]
    public void LinuxWithOldGlibc_NamesBothVersions()
    {
        var status = PlatformSupport.CheckLinux(new Version(2, 31));

        Assert.True(status.IsError);
        Assert.Equal("SharpDetect needs glibc 2.34 or newer, but this machine has glibc 2.31.", status.Error);
    }
}
