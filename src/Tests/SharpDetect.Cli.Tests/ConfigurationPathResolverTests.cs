// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using SharpDetect.Cli.Handlers;
using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class ConfigurationPathResolverTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "sharpdetect-paths"));
    private static readonly string ConfigurationDirectory = Path.Combine(Root, "B");

    private static string Expected(params string[] segments)
        => Path.GetFullPath(Path.Combine([ConfigurationDirectory, .. segments])).Replace(Path.DirectorySeparatorChar, '/');

    private static RunCommandArgs Deserialize(string json)
        => CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(json);

    [Fact]
    public void Resolve_RelativePaths_AreResolvedAgainstConfigurationDirectory()
    {
        var args = Deserialize(
            """
            {
              "Target": {
                "Path": "bin/app.dll",
                "WorkingDirectory": "../work",
                "RedirectInputOutput": {
                  "StdinFilePath": "in.txt",
                  "StdoutFilePath": "out/stdout.txt",
                  "StderrFilePath": "./stderr.txt"
                }
              },
              "Runtime": {
                "Host": { "Path": "tools/dotnet" },
                "Profiler": { "PathWindowsX64": "profiler/win.dll", "PathLinuxX64": "profiler/linux.so" }
              },
              "Analysis": {
                "PluginName": "FastTrack",
                "Path": "plugins/Plugins.dll",
                "ReportsFolder": "reports",
                "TemporaryFilesFolder": "tmp",
                "ReportFileName": "report.html"
              }
            }
            """);

        var resolved = ConfigurationPathResolver.Resolve(args, ConfigurationDirectory);

        Assert.Equal(Expected("bin", "app.dll"), resolved.Target.Path);
        Assert.Equal(Expected("..", "work"), resolved.Target.WorkingDirectory);
        Assert.Equal(Expected("in.txt"), resolved.Target.RedirectInputOutput!.StdinFilePath);
        Assert.Equal(Expected("out", "stdout.txt"), resolved.Target.RedirectInputOutput.StdoutFilePath);
        Assert.Equal(Expected("stderr.txt"), resolved.Target.RedirectInputOutput.StderrFilePath);
        Assert.Equal(Expected("tools", "dotnet"), resolved.Runtime.Host!.Path);
        Assert.Equal(Expected("profiler", "win.dll"), resolved.Runtime.Profiler.PathWindowsX64);
        Assert.Equal(Expected("profiler", "linux.so"), resolved.Runtime.Profiler.PathLinuxX64);
        Assert.Equal(Expected("plugins", "Plugins.dll"), resolved.Analysis.Path);
        Assert.Equal(Expected("reports"), resolved.Analysis.ReportsFolder!.Replace(Path.DirectorySeparatorChar, '/'));
        Assert.Equal(Expected("tmp"), resolved.Analysis.TemporaryFilesFolder!.Replace(Path.DirectorySeparatorChar, '/'));
        Assert.Equal("report.html", resolved.Analysis.ReportFileName);
    }

    [Fact]
    public void Resolve_KeepsAbsolutePathsDefaultsAndBareHostCommand()
    {
        var absoluteTarget = Path.Combine(Root, "elsewhere", "app.dll").Replace(Path.DirectorySeparatorChar, '/');
        var args = Deserialize(
            $$"""
            {
              "Target": { "Path": "{{absoluteTarget}}", "Args": [ "relative/not/a/path" ] },
              "Runtime": { "Host": { "Path": "dotnet" } },
              "Analysis": { "PluginName": "FastTrack" }
            }
            """);

        var resolved = ConfigurationPathResolver.Resolve(args, ConfigurationDirectory);

        Assert.Equal(absoluteTarget, resolved.Target.Path);
        Assert.Equal(new[] { "relative/not/a/path" }, resolved.Target.Args);
        Assert.Equal("dotnet", resolved.Runtime.Host!.Path);
        Assert.Null(resolved.Runtime.Profiler.PathLinuxX64Raw);
        Assert.Equal(args.Runtime.Profiler.PathLinuxX64, resolved.Runtime.Profiler.PathLinuxX64);
        Assert.Null(resolved.Analysis.PathRaw);
        Assert.Equal(args.Analysis.Path, resolved.Analysis.Path);
        Assert.Null(resolved.Target.WorkingDirectory);
        Assert.Null(resolved.Analysis.ReportsFolder);
    }

    [Theory]
    [InlineData("A", "bin/x.dll", "B", "../A/bin/x.dll")]
    [InlineData("A", "./bin/x.dll", "A", "bin/x.dll")]
    [InlineData("A", "x.dll", "A/B/C", "../../x.dll")]
    [InlineData("A", "%SHARPDETECT_ROOT%/x.dll", "B", "%SHARPDETECT_ROOT%/x.dll")]
    public void MakeRelative_RewritesPathRelativeToConfigurationDirectory(
        string currentDirectory, string path, string configurationDirectory, string expected)
    {
        var actual = ConfigurationPathResolver.MakeRelative(
            path,
            Path.Combine(Root, currentDirectory),
            Path.Combine(Root, configurationDirectory));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Init_FromAnotherDirectory_WritesTargetRelativeToConfigurationFile()
    {
        var json = new InitCommandHandler(
                outputFile: "../B/sd.json",
                new Commands.AnalysisOptions(PluginNameOrTypeFullName: "FastTrack", TargetPath: "./bin/x.dll"),
                currentDirectory: Path.Combine(Root, "A"))
            .CreateTemplateConfigurationJson();

        Assert.Contains("\"Path\": \"../A/bin/x.dll\"", json);
    }

    [Fact]
    public void Run_ConfigurationErrors_ShowResolvedPaths()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var configurationFile = Path.Combine(directory.FullName, "sd.json");
            File.WriteAllText(
                configurationFile,
                """{ "Target": { "Path": "bin/missing.dll" }, "Analysis": { "PluginName": "FastTrack" } }""");

            var exception = Assert.Throws<CommandException>(() => new RunCommandHandler(configurationFile));

            var expectedPath = Path.Combine(directory.FullName, "bin", "missing.dll").Replace(Path.DirectorySeparatorChar, '/');
            Assert.Contains($"Could not find target assembly: \"{expectedPath}\"", exception.Message);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
