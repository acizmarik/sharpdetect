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
        var command = new RunCommand { PluginType = "FastTrack", TargetPath = "app.dll" };

        var exception = Record.Exception(command.ValidateOptions);

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_InlineFormWithTest_DoesNotThrow()
    {
        var command = new RunCommand
        {
            PluginType = "FastTrack",
            TargetPath = "tests.dll",
            IsTest = true,
            TestFilter = "FullyQualifiedName~MyNamespace.RaceFact",
            TestRunner = TestRunner.Mtp
        };

        var exception = Record.Exception(command.ValidateOptions);

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_InlineFormWithoutPlugin_DefaultsToFastTrack()
    {
        var command = new RunCommand { TargetPath = "app.dll" };

        command.ValidateOptions();
        var options = command.ToAnalysisOptions();

        Assert.Equal("FastTrack", options.PluginNameOrTypeFullName);
    }

    [Theory]
    [InlineData("--plugin")]
    [InlineData("--target")]
    [InlineData("--instrument-system-libraries")]
    [InlineData("--stack-traces")]
    public void Validate_ConfigFileWithInlineOption_ThrowsConfigurationError(string option)
    {
        var configurationFile = Path.GetTempFileName();
        try
        {
            var command = new RunCommand { ConfigurationFile = configurationFile };
            switch (option)
            {
                case "--plugin": command.PluginType = "FastTrack"; break;
                case "--target": command.TargetPath = "app.dll"; break;
                case "--instrument-system-libraries": command.InstrumentSystemLibraries = true; break;
                case "--stack-traces": command.CollectStackTraces = true; break;
            }

            var exception = Assert.Throws<CommandException>(command.ValidateOptions);

            Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
            Assert.Contains($"{option} cannot be combined with a configuration file", exception.Message);
        }
        finally
        {
            File.Delete(configurationFile);
        }
    }

    [Fact]
    public void Validate_NeitherConfigFileNorInlineOptions_ThrowsConfigurationError()
    {
        var command = new RunCommand();

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_InlineFormMissingTarget_ThrowsConfigurationError()
    {
        var command = new RunCommand { PluginType = "FastTrack" };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_MissingConfigFile_ThrowsConfigurationError()
    {
        var command = new RunCommand { ConfigurationFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json") };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_FilterWithoutTest_ThrowsConfigurationError()
    {
        var command = new RunCommand { PluginType = "FastTrack", TargetPath = "app.dll", TestFilter = "FullyQualifiedName~Something" };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Theory]
    [InlineData(TestRunner.Mtp)]
    [InlineData(TestRunner.VsTest)]
    public void Validate_RunnerWithoutTest_ThrowsConfigurationError(TestRunner testRunner)
    {
        var command = new RunCommand { PluginType = "FastTrack", TargetPath = "app.dll", TestRunner = testRunner };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
        Assert.Contains("--runner requires --test", exception.Message);
    }

    [Fact]
    public void Validate_StackTraceDepthWithoutStackTraces_ThrowsConfigurationError()
    {
        var command = new RunCommand { TargetPath = "app.dll", StackTraceDepth = 4 };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
        Assert.Contains("--stack-trace-depth requires --stack-traces", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveStackTraceDepth_ThrowsConfigurationError(int depth)
    {
        var command = new RunCommand { TargetPath = "app.dll", CollectStackTraces = true, StackTraceDepth = depth };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }
}
