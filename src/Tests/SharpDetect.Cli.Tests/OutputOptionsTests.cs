// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using Microsoft.Extensions.Logging;
using SharpDetect.Cli.Commands;
using SharpDetect.Cli.Handlers;
using SharpDetect.Cli.Output;
using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class OutputOptionsTests
{
    private static RunCommandArgs CreateConfiguration()
        => CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(
            """
            {
              "Target": { "Path": "app.dll" },
              "Runtime": { "Profiler": { "LogLevel": "Information" } },
              "Analysis": {
                "PluginName": "FastTrack",
                "RenderReport": true,
                "LogLevel": "Debug",
                "ReportsFolder": "/configured",
                "ReportFileName": "configured.html"
              }
            }
            """);

    [Theory]
    [InlineData(Verbosity.Quiet, LogLevel.Error, ProfilerLogLevel.Error)]
    [InlineData(Verbosity.Normal, LogLevel.Warning, ProfilerLogLevel.Warning)]
    [InlineData(Verbosity.Detailed, LogLevel.Information, ProfilerLogLevel.Warning)]
    [InlineData(Verbosity.Diagnostic, LogLevel.Debug, ProfilerLogLevel.Information)]
    public void Verbosity_SetsAnalysisAndProfilerLogLevels(Verbosity verbosity, LogLevel analysis, ProfilerLogLevel profiler)
    {
        var arguments = new OutputOptions(Verbosity: verbosity).ApplyTo(CreateConfiguration());

        Assert.Equal(analysis, arguments.Analysis.LogLevel);
        Assert.Equal(profiler, arguments.Runtime.Profiler.LogLevel);
        Assert.Equal(verbosity, VerbosityExtensions.FromAnalysisLogLevel(arguments.Analysis.LogLevel));
    }

    [Fact]
    public void NothingSpecified_KeepsConfiguredValues()
    {
        var configuration = CreateConfiguration();

        var arguments = OutputOptions.Default.ApplyTo(configuration);

        Assert.Equal(LogLevel.Debug, arguments.Analysis.LogLevel);
        Assert.Equal(ProfilerLogLevel.Information, arguments.Runtime.Profiler.LogLevel);
        Assert.True(arguments.Analysis.RenderReport);
        Assert.Equal("/configured", arguments.Analysis.ReportsFolder);
        Assert.Equal("configured.html", arguments.Analysis.ReportFileName);
        Assert.Equal(configuration.Analysis.Path, arguments.Analysis.Path);
        Assert.Equal(configuration.Analysis.PluginName, arguments.Analysis.PluginName);
    }

    [Theory]
    [InlineData("out/race.html", "out", "race.html")]
    [InlineData("race.HTML", ".", "race.HTML")]
    [InlineData("out/reports", "out/reports", null)]
    public void ReportPath_NamesFileOrFolder(string reportPath, string? folder, string? fileName)
    {
        var arguments = new OutputOptions(ReportPath: reportPath).ApplyTo(CreateConfiguration());

        Assert.True(arguments.Analysis.RenderReport);
        Assert.Equal(folder, arguments.Analysis.ReportsFolder?.Replace(Path.DirectorySeparatorChar, '/'));
        Assert.Equal(fileName, arguments.Analysis.ReportFileName);
    }

    [Fact]
    public void NoReport_DisablesTheReport()
    {
        var arguments = new OutputOptions(NoReport: true).ApplyTo(CreateConfiguration());

        Assert.False(arguments.Analysis.RenderReport);
    }

    [Fact]
    public void ReportAndNoReport_AreRejected()
    {
        var command = new RunCommand(PassThroughArguments.None) { TargetPath = "app.dll", ReportPath = "out.html", NoReport = true };

        var exception = Assert.Throws<CommandException>(command.ValidateOptions);

        Assert.Contains("--report cannot be combined with --no-report", exception.Message);
    }

    [Fact]
    public void OutputOptions_AreAllowedWithConfigurationFile()
    {
        var configurationFile = Path.GetTempFileName();
        try
        {
            var command = new RunCommand(PassThroughArguments.None)
            {
                ConfigurationFile = configurationFile,
                Verbosity = Verbosity.Quiet,
                ReportPath = "out.html"
            };

            var exception = Record.Exception(command.ValidateOptions);

            Assert.Null(exception);
        }
        finally
        {
            File.Delete(configurationFile);
        }
    }

    [Fact]
    public void Init_WritesOutputOptions()
    {
        var root = Path.Combine(Path.GetTempPath(), "sharpdetect-output");
        var json = new InitCommandHandler(
                outputFile: "../B/sd.json",
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "FastTrack",
                    TargetPath: "app.dll",
                    Output: new OutputOptions(Verbosity: Verbosity.Detailed, ReportPath: "reports")),
                currentDirectory: Path.Combine(root, "A"))
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"LogLevel\": \"Information\"", json);
        Assert.Contains("\"ReportsFolder\": \"../A/reports\"", json);
        Assert.Contains("\"RenderReport\": true", json);
    }

    [Fact]
    public void Init_ReportFileNextToConfiguration_StaysRelativeToConfiguration()
    {
        var directory = Path.Combine(Path.GetTempPath(), "sharpdetect-output", "A");
        var json = new InitCommandHandler(
                outputFile: "sd.json",
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "FastTrack",
                    TargetPath: "app.dll",
                    Output: new OutputOptions(ReportPath: "r.html")),
                currentDirectory: directory)
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"ReportsFolder\": \".\"", json);
        Assert.Contains("\"ReportFileName\": \"r.html\"", json);

        var arguments = ConfigurationPathResolver.Resolve(
            CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(json),
            directory);
        Assert.Equal(directory, arguments.Analysis.ReportsFolder);
    }

    [Fact]
    public void Init_NoReport_WritesRenderReportFalse()
    {
        var json = new InitCommandHandler(
                outputFile: "sd.json",
                new AnalysisOptions(
                    PluginNameOrTypeFullName: "FastTrack",
                    TargetPath: "app.dll",
                    Output: new OutputOptions(NoReport: true)))
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"RenderReport\": false", json);
    }
}
