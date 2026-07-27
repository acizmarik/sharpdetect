// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class CommandDeserializerTests
{
    private static string DeserializeAndCaptureError(string configuration)
    {
        var exception = Record.Exception(() => CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(configuration));
        Assert.NotNull(exception);
        return ExceptionMessages.Flatten(exception);
    }

    [Fact]
    public void Deserialize_InvalidEnumValue_NamesTheProperty()
    {
        var message = DeserializeAndCaptureError(
            """
            {
              "Target": { "Path": "app.dll", "Kind": "Nonsense" },
              "Analysis": { "PluginName": "FastTrack" }
            }
            """);

        Assert.Contains("Nonsense", message);
        Assert.Contains("$.Target.Kind", message);
    }

    [Fact]
    public void Deserialize_WrongPropertyType_NamesTheProperty()
    {
        var message = DeserializeAndCaptureError(
            """
            {
              "Target": { "Path": 42 },
              "Analysis": { "PluginName": "FastTrack" }
            }
            """);

        Assert.Contains("$.Target.Path", message);
    }

    [Fact]
    public void Deserialize_UnknownLogLevel_NamesTheProperty()
    {
        var message = DeserializeAndCaptureError(
            """
            {
              "Target": { "Path": "app.dll" },
              "Analysis": { "PluginName": "FastTrack", "LogLevel": "Verbose" }
            }
            """);

        Assert.Contains("Verbose", message);
        Assert.Contains("$.Analysis.LogLevel", message);
    }

    [Fact]
    public void Deserialize_UnknownPropertyInSection_IsRejected()
    {
        var message = DeserializeAndCaptureError(
            """
            {
              "Target": { "Path": "app.dll", "Typo": 1 },
              "Analysis": { "PluginName": "FastTrack" }
            }
            """);

        Assert.Contains("Typo", message);
    }

    [Fact]
    public void Deserialize_UnknownTopLevelProperty_IsRejected()
    {
        var message = DeserializeAndCaptureError(
            """
            {
              "Target": { "Path": "app.dll" },
              "Analysis": { "PluginName": "FastTrack" },
              "Anaylsis": { }
            }
            """);

        Assert.Contains("Anaylsis", message);
    }

    [Fact]
    public void Deserialize_AnalysisPath_IsHonored()
    {
        var arguments = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(
            """
            {
              "Target": { "Path": "app.dll" },
              "Analysis": { "PluginName": "FastTrack", "Path": "/plugins/Custom.dll" }
            }
            """);

        Assert.Equal("/plugins/Custom.dll", arguments.Analysis.Path);
    }

    [Fact]
    public void Deserialize_ProfilerSettings_AreHonored()
    {
        var arguments = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(
            """
            {
              "Target": { "Path": "app.dll" },
              "Analysis": { "PluginName": "FastTrack" },
              "Runtime": {
                "Profiler": {
                  "Clsid": "{11111111-2222-3333-4444-555555555555}",
                  "PathLinuxX64": "/profilers/Custom.so",
                  "PathWindowsX64": "C:/profilers/Custom.dll"
                }
              }
            }
            """);

        Assert.Equal("{11111111-2222-3333-4444-555555555555}", arguments.Runtime.Profiler.Clsid);
        Assert.Equal("/profilers/Custom.so", arguments.Runtime.Profiler.PathLinuxX64);
        Assert.Equal("C:/profilers/Custom.dll", arguments.Runtime.Profiler.PathWindowsX64);
    }

    [Fact]
    public void Deserialize_EnvironmentVariables_AreExpanded()
    {
        const string variableName = "SHARPDETECT_TEST_CONFIG_DIR";
        Environment.SetEnvironmentVariable(variableName, "/expanded");

        try
        {
            var arguments = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(
                $$"""
                  {
                    "Target": { "Path": "%{{variableName}}%/app.dll" },
                    "Analysis": { "PluginName": "FastTrack", "ReportsFolder": "%{{variableName}}%/reports" }
                  }
                  """);

            Assert.Equal("/expanded/app.dll", arguments.Target.Path);
            Assert.Equal("/expanded/reports", arguments.Analysis.ReportsFolder);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [Fact]
    public void Deserialize_ValidConfiguration_IsAccepted()
    {
        var arguments = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(
            """
            {
              "Target": { "Path": "app.dll", "WorkingDirectory": "/tmp" },
              "Analysis": { "PluginName": "FastTrack", "LogLevel": "Debug" }
            }
            """);

        Assert.Equal("app.dll", arguments.Target.Path);
        Assert.Equal("/tmp", arguments.Target.WorkingDirectory);
        Assert.Equal("FastTrack", arguments.Analysis.PluginName);
    }
}
