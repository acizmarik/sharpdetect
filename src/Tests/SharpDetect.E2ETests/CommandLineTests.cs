// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.E2ETests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection(CollectionName)]
public class CommandLineTests(ITestOutputHelper testOutput) : IDisposable
{
    public const string CollectionName = "E2E_CommandLineTests";
    private const string Sdk = "net10.0";

    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("sharpdetect-cli-");

    [Fact]
    public async Task PassThroughArguments_ReachTargetAsSeparateArguments()
    {
        // Arrange
        var echoFile = Path.Combine(_directory.FullName, "echo.txt");

        // Act
        var result = await CliRunner.RunAsync(
            ["run", "-t", CliRunner.GetSubjectPath(Sdk), "--", "Test_EchoArguments", echoFile, "two words", "--flag", "--", ""],
            testOutput);
        DeleteReport(result);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(["two words", "--flag", "--", ""], await File.ReadAllLinesAsync(echoFile));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfigurationFileArguments_ReachTargetAsSeparateArguments(bool useArrayForm)
    {
        // Arrange
        var echoFile = Path.Combine(_directory.FullName, "echo.txt").Replace('\\', '/');
        var args = useArrayForm
            ? $$"""[ "Test_EchoArguments", "{{echoFile}}", "two words", "--flag" ]"""
            : $$"""
              "Test_EchoArguments \"{{echoFile}}\" \"two words\" --flag"
              """;
        var configurationFile = WriteConfiguration(
            $$"""
            {
              "Target": {
                "Path": "{{CliRunner.GetSubjectPath(Sdk).Replace('\\', '/')}}",
                "Args": {{args}}
              },
              "Analysis": { "PluginName": "FastTrack", "RenderReport": false }
            }
            """);

        // Act
        var result = await CliRunner.RunAsync(["run", configurationFile], testOutput);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(["two words", "--flag"], await File.ReadAllLinesAsync(echoFile));
    }

    private string WriteConfiguration(string json)
    {
        var path = Path.Combine(_directory.FullName, "sharpdetect.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static void DeleteReport(CliRunResult result)
    {
        if (result.ReportPath is { } reportPath && File.Exists(reportPath))
            File.Delete(reportPath);
    }

    public void Dispose()
    {
        _directory.Delete(recursive: true);
    }
}
