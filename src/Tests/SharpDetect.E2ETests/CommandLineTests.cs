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
            ["run", "-t", CliRunner.GetSubjectPath(Sdk), "--no-report", "--", "Test_EchoArguments", echoFile, "two words", "--flag", "--", ""],
            testOutput);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(["two words", "--flag", "--", ""], await File.ReadAllLinesAsync(echoFile));
        Assert.Null(result.ReportPath);
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

    [Fact]
    public async Task ConfigurationFile_PathsAreRelativeToTheFile()
    {
        // Arrange
        var configurationDirectory = _directory.CreateSubdirectory("config");
        configurationDirectory.CreateSubdirectory("work");
        var subjectPath = Path.GetRelativePath(configurationDirectory.FullName, CliRunner.GetSubjectPath(Sdk)).Replace('\\', '/');
        var configurationFile = Path.Combine(configurationDirectory.FullName, "sharpdetect.json");
        await File.WriteAllTextAsync(
            configurationFile,
            $$"""
            {
              "Target": {
                "Path": "{{subjectPath}}",
                "Args": [ "Test_EchoArguments", "echo.txt", "x" ],
                "WorkingDirectory": "work"
              },
              "Analysis": { "PluginName": "FastTrack", "RenderReport": true, "ReportsFolder": "reports" }
            }
            """);

        // Act
        var result = await CliRunner.RunAsync(["run", configurationFile], testOutput);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(["x"], await File.ReadAllLinesAsync(Path.Combine(configurationDirectory.FullName, "work", "echo.txt")));
        Assert.NotNull(result.ReportPath);
        Assert.Equal(Path.Combine(configurationDirectory.FullName, "reports"), Path.GetDirectoryName(result.ReportPath));
        Assert.True(File.Exists(result.ReportPath));
    }

    [Fact]
    public async Task ConfigurationFile_DefaultsToTheInvokingDirectory()
    {
        // Arrange
        var echoFileName = $"echo-{Guid.NewGuid():N}.txt";
        var echoFile = Path.Combine(Directory.GetCurrentDirectory(), echoFileName);
        var configurationFile = WriteConfiguration(
            $$"""
            {
              "Target": {
                "Path": "{{CliRunner.GetSubjectPath(Sdk).Replace('\\', '/')}}",
                "Args": [ "Test_EchoArguments", "{{echoFileName}}", "x" ]
              },
              "Analysis": { "PluginName": "FastTrack", "RenderReport": true }
            }
            """);

        try
        {
            // Act
            var result = await CliRunner.RunAsync(["run", configurationFile], testOutput);
            DeleteReport(result);

            // Assert
            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(echoFile), "The target did not run in the invoking directory.");
            Assert.Equal(Directory.GetCurrentDirectory(), Path.GetDirectoryName(result.ReportPath));
        }
        finally
        {
            File.Delete(echoFile);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigurationFile_ReportOptionsOverrideTheFile(bool noReport)
    {
        // Arrange
        var reportPath = Path.Combine(_directory.FullName, "custom", "race.html");
        var configurationFile = WriteConfiguration(
            $$"""
            {
              "Target": {
                "Path": "{{CliRunner.GetSubjectPath(Sdk).Replace('\\', '/')}}",
                "Args": [ "Test_DataRace_ReferenceType_Static_ReadWriteRace" ]
              },
              "Analysis": { "PluginName": "FastTrack", "RenderReport": true, "ReportsFolder": "reports" }
            }
            """);

        // Act
        string[] reportOptions = noReport ? ["--no-report"] : ["--report", reportPath];
        var result = await CliRunner.RunAsync(["run", configurationFile, "-v", "quiet", .. reportOptions], testOutput);

        // Assert
        Assert.Equal(5, result.ExitCode);
        Assert.False(Directory.Exists(Path.Combine(_directory.FullName, "reports")));
        if (noReport)
        {
            Assert.Null(result.ReportPath);
            Assert.False(File.Exists(reportPath));
        }
        else
        {
            Assert.Equal(reportPath, result.ReportPath);
            Assert.True(File.Exists(reportPath));
        }
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
