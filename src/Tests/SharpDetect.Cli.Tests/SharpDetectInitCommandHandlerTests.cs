// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx.Infrastructure;
using SharpDetect.Cli.Commands;
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
                "Args": [],
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
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "TestPlugin",
                    TargetPath: "TestTarget.dll",
                    IsTest: false,
                    TestRunner: null,
                    TestFilter: null,
                    InstrumentSystemLibraries: false))
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
                "Args": [],
                "Test": {
                  "Runner": "Mtp",
                  "Filter": "FullyQualifiedName~MyNamespace.RaceFact",
                  "AdditionalRunnerArgs": []
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
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "TestPlugin",
                    TargetPath: "TestTarget.dll",
                    IsTest: true,
                    TestRunner: TestRunner.Mtp,
                    TestFilter: "FullyQualifiedName~MyNamespace.RaceFact",
                    InstrumentSystemLibraries: false))
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
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "FastTrack",
                    TargetPath: "TestTarget.dll",
                    IsTest: false,
                    TestRunner: null,
                    TestFilter: null,
                    InstrumentSystemLibraries: false))
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
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "FastTrack",
                    TargetPath: "TestTarget.dll",
                    IsTest: true,
                    TestRunner: TestRunner.Mtp,
                    TestFilter: "FullyQualifiedName~MyNamespace.RaceFact",
                    InstrumentSystemLibraries: false))
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
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "FastTrack",
                    TargetPath: "TestTarget.dll",
                    IsTest: false,
                    TestRunner: null,
                    TestFilter: null,
                    InstrumentSystemLibraries: true))
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"SkipInstrumentationForAssemblies\": []", actualTemplate);
        Assert.DoesNotContain("\"System.\"", actualTemplate);
        Assert.DoesNotContain("\"Microsoft.\"", actualTemplate);
    }

    [Fact]
    public void Init_StackTraces_EnablesCollectionWithDepth()
    {
        var actualTemplate =
            new InitCommandHandler(
                outputFile: "TestConfig.json",
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "FastTrack",
                    TargetPath: "TestTarget.dll",
                    CollectStackTraces: true,
                    StackTraceDepth: 16))
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"EnableStackTraceCollection\": true", actualTemplate);
        Assert.Contains("\"StackTraceCollectionMaxDepth\": 16", actualTemplate);
    }

    [Fact]
    public void Init_StackTracesWithoutDepth_KeepsPluginDefaultDepth()
    {
        var actualTemplate =
            new InitCommandHandler(
                outputFile: "TestConfig.json",
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "FastTrack",
                    TargetPath: "TestTarget.dll",
                    CollectStackTraces: true))
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"EnableStackTraceCollection\": true", actualTemplate);
        Assert.DoesNotContain("StackTraceCollectionMaxDepth", actualTemplate);
    }

    [Fact]
    public void Init_WithoutStackTraces_DoesNotMentionStackTraces()
    {
        var actualTemplate =
            new InitCommandHandler(
                outputFile: "TestConfig.json",
                new AnalysisOptions(PluginNameOrTypeFullName: "FastTrack", TargetPath: "TestTarget.dll"))
            .CreateTemplateConfigurationJson();

        Assert.DoesNotContain("StackTrace", actualTemplate);
    }

    [Fact]
    public async Task Init_WithoutPlugin_WritesFastTrack()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var outputFile = Path.Combine(directory.FullName, "sd.json");
            using var console = new FakeInMemoryConsole();
            var application = Program.CreateApplicationBuilder().UseConsole(console).Build();

            var exitCode = await application.RunAsync(["init", "-t", typeof(InitCommandHandler).Assembly.Location, "-o", outputFile]);

            Assert.Equal(0, exitCode);
            Assert.Contains("\"PluginName\": \"FastTrack\"", await File.ReadAllTextAsync(outputFile));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
