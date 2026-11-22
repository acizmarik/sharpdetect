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
        var command = new RunCommand(PassThroughArguments.None) { PluginType = "FastTrack", TargetPath = "app.dll" };

        var exception = Record.Exception(command.ValidateOptions);

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_InlineFormWithTest_DoesNotThrow()
    {
        var command = new RunCommand(PassThroughArguments.None)
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
        var command = new RunCommand(PassThroughArguments.None) { TargetPath = "app.dll" };

        command.ValidateOptions();
        var options = command.ToAnalysisOptions();

        Assert.Equal("FastTrack", options.PluginNameOrTypeFullName);
    }

    [Theory]
    [InlineData("--plugin")]
    [InlineData("--target")]
    [InlineData("--instrument-system-libraries")]
    [InlineData("--no-stack-traces")]
    [InlineData("--stack-trace-depth")]
    public void Validate_ConfigFileWithInlineOption_ThrowsConfigurationError(string option)
    {
        var configurationFile = Path.GetTempFileName();
        try
        {
            var command = new RunCommand(PassThroughArguments.None) { ConfigurationFile = configurationFile };
            switch (option)
            {
                case "--plugin": command.PluginType = "FastTrack"; break;
                case "--target": command.TargetPath = "app.dll"; break;
                case "--instrument-system-libraries": command.InstrumentSystemLibraries = true; break;
                case "--no-stack-traces": command.NoStackTraces = true; break;
                case "--stack-trace-depth": command.StackTraceDepth = 4; break;
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
        var command = new RunCommand(PassThroughArguments.None);

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_InlineFormMissingTarget_ThrowsConfigurationError()
    {
        var command = new RunCommand(PassThroughArguments.None) { PluginType = "FastTrack" };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_MissingConfigFile_ThrowsConfigurationError()
    {
        var command = new RunCommand(PassThroughArguments.None) { ConfigurationFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json") };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Fact]
    public void Validate_FilterWithoutTest_ThrowsConfigurationError()
    {
        var command = new RunCommand(PassThroughArguments.None) { PluginType = "FastTrack", TargetPath = "app.dll", TestFilter = "FullyQualifiedName~Something" };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }

    [Theory]
    [InlineData(TestRunner.Mtp)]
    [InlineData(TestRunner.VsTest)]
    public void Validate_RunnerWithoutTest_ThrowsConfigurationError(TestRunner testRunner)
    {
        var command = new RunCommand(PassThroughArguments.None) { PluginType = "FastTrack", TargetPath = "app.dll", TestRunner = testRunner };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
        Assert.Contains("--runner requires --test", exception.Message);
    }

    [Fact]
    public void Validate_StackTraceDepthWithNoStackTraces_ThrowsConfigurationError()
    {
        var command = new RunCommand(PassThroughArguments.None) { TargetPath = "app.dll", NoStackTraces = true, StackTraceDepth = 4 };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
        Assert.Contains("--stack-trace-depth cannot be combined with --no-stack-traces", exception.Message);
    }

    [Fact]
    public void ToAnalysisOptions_ByDefault_CollectsStackTraces()
    {
        var command = new RunCommand(PassThroughArguments.None) { TargetPath = "app.dll", StackTraceDepth = 4 };

        command.ValidateOptions();
        var options = command.ToAnalysisOptions();

        Assert.True(options.CollectStackTraces);
        Assert.Equal(4, options.StackTraceDepth);
    }

    [Fact]
    public void ToAnalysisOptions_NoStackTraces_DisablesCollection()
    {
        var command = new RunCommand(PassThroughArguments.None) { TargetPath = "app.dll", NoStackTraces = true };

        command.ValidateOptions();
        var options = command.ToAnalysisOptions();

        Assert.False(options.CollectStackTraces);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveStackTraceDepth_ThrowsConfigurationError(int depth)
    {
        var command = new RunCommand(PassThroughArguments.None) { TargetPath = "app.dll", StackTraceDepth = depth };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Equal((int)ExitCode.ConfigurationError, exception.ExitCode);
    }
}
