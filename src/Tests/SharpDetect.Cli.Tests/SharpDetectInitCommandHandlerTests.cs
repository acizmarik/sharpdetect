// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Cli.Handlers;
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
                targetAssemblyPath: "TestTarget.dll")
            .CreateTemplateConfigurationJson()
            .ReplaceLineEndings();
        
        Assert.Equal(expectedTemplate, actualTemplate);
    }
}