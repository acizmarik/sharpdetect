// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using SharpDetect.Cli.Commands;
using SharpDetect.Worker.Commands.Run;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class SharpDetectRunCommandTests
{
    [Fact]
    public void Validate_InlineForm_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            RunCommand.ValidateOptions(
                argumentsFile: null,
                pluginType: "FastTrack",
                targetPath: "app.dll",
                isTest: false,
                testFilter: null,
                testRunner: TestTargetConfigurationArgs.DefaultRunner));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_InlineFormWithTest_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            RunCommand.ValidateOptions(
                argumentsFile: null,
                pluginType: "FastTrack",
                targetPath: "tests.dll",
                isTest: true,
                testFilter: "FullyQualifiedName~MyNamespace.RaceFact",
                testRunner: TestRunner.Mtp));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_BothConfigFileAndInlineOptions_ThrowsConfigurationError()
    {
        var exception = Assert.Throws<CommandException>(() =>
            RunCommand.ValidateOptions(
                argumentsFile: "config.json",
                pluginType: "FastTrack",
                targetPath: "app.dll",
                isTest: false,
                testFilter: null,
                testRunner: TestTargetConfigurationArgs.DefaultRunner));

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_NeitherConfigFileNorInlineOptions_ThrowsConfigurationError()
    {
        var exception = Assert.Throws<CommandException>(() =>
            RunCommand.ValidateOptions(
                argumentsFile: null,
                pluginType: null,
                targetPath: null,
                isTest: false,
                testFilter: null,
                testRunner: TestTargetConfigurationArgs.DefaultRunner));

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_InlineFormMissingTarget_ThrowsConfigurationError()
    {
        var exception = Assert.Throws<CommandException>(() =>
            RunCommand.ValidateOptions(
                argumentsFile: null,
                pluginType: "FastTrack",
                targetPath: null,
                isTest: false,
                testFilter: null,
                testRunner: TestTargetConfigurationArgs.DefaultRunner));

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_InlineFormMissingPlugin_ThrowsConfigurationError()
    {
        var exception = Assert.Throws<CommandException>(() =>
            RunCommand.ValidateOptions(
                argumentsFile: null,
                pluginType: null,
                targetPath: "app.dll",
                isTest: false,
                testFilter: null,
                testRunner: TestTargetConfigurationArgs.DefaultRunner));

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_FilterWithoutTest_ThrowsConfigurationError()
    {
        var exception = Assert.Throws<CommandException>(() =>
            RunCommand.ValidateOptions(
                argumentsFile: null,
                pluginType: "FastTrack",
                targetPath: "app.dll",
                isTest: false,
                testFilter: "FullyQualifiedName~Something",
                testRunner: TestTargetConfigurationArgs.DefaultRunner));

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }
}
