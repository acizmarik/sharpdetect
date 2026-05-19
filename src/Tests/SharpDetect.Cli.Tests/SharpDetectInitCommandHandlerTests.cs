// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Cli.Handlers;
using SharpDetect.Worker.Commands.Run;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class SharpDetectInitCommandHandlerTests
{
    [Fact]
    public void Init_Should_Create_DefaultTemplate()
    {
        var expectedTemplate =
            """
            {
              "Target": {
                "Path": "TestTarget.dll",
                "RedirectInputOutput": {
                  "SingleConsoleMode": true
                }
              },
              "Analysis": {
                "Configuration": {
                  "SkipInstrumentationForAssemblies": [
                    "System.",
                    "Microsoft."
                  ]
                },
                "PluginName": "TestPlugin",
                "RenderReport": true,
                "LogLevel": "Warning"
              },
              "Runtime": {
                "Profiler": {
                  "LogLevel": "Warning"
                }
              }
            }
            """
            .ReplaceLineEndings();
        
        var actualTemplate = 
            new InitCommandHandler(
                outputFile: "TestConfig.json", 
                pluginNameOrTypeFullName: "TestPlugin", 
                targetAssemblyPath: "TestTarget.dll",
                instrumentSystemLibraries: false,
                isTest: false,
                testRunner: null,
                testFilter: null)
            .CreateTemplateConfigurationJson()
            .ReplaceLineEndings();
        
        Assert.Equal(expectedTemplate, actualTemplate);
    }

    [Fact]
    public void Init_WithTestFlag_Should_Create_TestTemplate()
    {
        var expectedTemplate =
            """
            {
              "Target": {
                "Path": "TestTarget.dll",
                "Kind": "TestAssembly",
                "Test": {
                  "Runner": "Mtp",
                  "Filter": "FullyQualifiedName~MyNamespace.RaceFact"
                }
              },
              "Analysis": {
                "Configuration": {
                  "SkipInstrumentationForAssemblies": [
                    "System.",
                    "Microsoft.",
                    "xunit.",
                    "nunit.",
                    "TUnit."
                  ]
                },
                "PluginName": "TestPlugin",
                "RenderReport": true,
                "LogLevel": "Warning"
              },
              "Runtime": {
                "Profiler": {
                  "LogLevel": "Warning"
                }
              }
            }
            """
            .ReplaceLineEndings();

        var actualTemplate =
            new InitCommandHandler(
                outputFile: "TestConfig.json",
                pluginNameOrTypeFullName: "TestPlugin",
                targetAssemblyPath: "TestTarget.dll",
                instrumentSystemLibraries: false,
                isTest: true,
                testRunner: TestRunner.Mtp,
                testFilter: "FullyQualifiedName~MyNamespace.RaceFact")
            .CreateTemplateConfigurationJson()
            .ReplaceLineEndings();

        Assert.Equal(expectedTemplate, actualTemplate);
    }

    [Fact]
    public void Init_FastTrackPlugin_EmitsBaseSkipList()
    {
        var actualTemplate =
            new InitCommandHandler(
                outputFile: "TestConfig.json",
                pluginNameOrTypeFullName: "FastTrack",
                targetAssemblyPath: "TestTarget.dll",
                instrumentSystemLibraries: false,
                isTest: false,
                testRunner: null,
                testFilter: null)
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"System.\"", actualTemplate);
        Assert.Contains("\"Microsoft.\"", actualTemplate);
        Assert.DoesNotContain("xunit.", actualTemplate);
        Assert.DoesNotContain("nunit.", actualTemplate);
        Assert.DoesNotContain("TUnit.", actualTemplate);
    }

    [Fact]
    public void Init_FastTrackPlugin_TestMode_EmitsExtendedSkipList()
    {
        var actualTemplate =
            new InitCommandHandler(
                outputFile: "TestConfig.json",
                pluginNameOrTypeFullName: "FastTrack",
                targetAssemblyPath: "TestTarget.dll",
                instrumentSystemLibraries: false,
                isTest: true,
                testRunner: TestRunner.Mtp,
                testFilter: "FullyQualifiedName~MyNamespace.RaceFact")
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"System.\"", actualTemplate);
        Assert.Contains("\"Microsoft.\"", actualTemplate);
        Assert.Contains("\"xunit.\"", actualTemplate);
        Assert.Contains("\"nunit.\"", actualTemplate);
        Assert.Contains("\"TUnit.\"", actualTemplate);
    }

    [Fact]
    public void Init_FastTrackPlugin_InstrumentSystemLibraries_EmitsEmptyList()
    {
        var actualTemplate =
            new InitCommandHandler(
                outputFile: "TestConfig.json",
                pluginNameOrTypeFullName: "FastTrack",
                targetAssemblyPath: "TestTarget.dll",
                instrumentSystemLibraries: true,
                isTest: false,
                testRunner: null,
                testFilter: null)
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"SkipInstrumentationForAssemblies\": []", actualTemplate);
        Assert.DoesNotContain("\"System.\"", actualTemplate);
        Assert.DoesNotContain("\"Microsoft.\"", actualTemplate);
    }

    [Fact]
    public void Init_EraserPlugin_EmitsBaseSkipList()
    {
        var actualTemplate =
            new InitCommandHandler(
                outputFile: "TestConfig.json",
                pluginNameOrTypeFullName: "Eraser",
                targetAssemblyPath: "TestTarget.dll",
                instrumentSystemLibraries: false,
                isTest: false,
                testRunner: null,
                testFilter: null)
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"Configuration\": {", actualTemplate);
        Assert.Contains("\"System.\"", actualTemplate);
        Assert.Contains("\"Microsoft.\"", actualTemplate);
    }
}