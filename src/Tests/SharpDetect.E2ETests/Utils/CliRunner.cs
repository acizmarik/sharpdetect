// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using CliFx.Infrastructure;
using SharpDetect.Cli;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests.Utils;

internal sealed record CliRunResult(int ExitCode, string Output, string Error, string? ReportPath);

internal static partial class CliRunner
{
    private const string SubjectRelativePath =
        "../../../../SharpDetect.E2ETests.Subject/bin/%BUILD_CONFIGURATION%/%SDK%/SharpDetect.E2ETests.Subject.dll";

#if DEBUG
    private const string BuildConfiguration = "Debug";
#elif RELEASE
    private const string BuildConfiguration = "Release";
#else
#error Unknown build configuration. Expected DEBUG or RELEASE.
#endif

    public static string GetSubjectPath(string sdk)
    {
        return Path.GetFullPath(SubjectRelativePath
            .Replace("%SDK%", sdk)
            .Replace("%BUILD_CONFIGURATION%", BuildConfiguration));
    }

    public static async Task<CliRunResult> RunAsync(IReadOnlyList<string> commandLine, ITestOutputHelper testOutput)
    {
        using var console = new FakeInMemoryConsole();
        var exitCode = await Program.RunAsync(commandLine, console);
        var output = console.ReadOutputString();
        var error = console.ReadErrorString();
        testOutput.WriteLine($"sharpdetect {string.Join(' ', commandLine)} -> {exitCode}");
        testOutput.WriteLine(output);
        testOutput.WriteLine(error);

        var reportMatch = ReportPathRegex().Match(output);
        return new CliRunResult(exitCode, output, error, reportMatch.Success ? reportMatch.Groups[1].Value : null);
    }

    [GeneratedRegex(@"Report stored to file: (.+\.html)", RegexOptions.CultureInvariant)]
    private static partial Regex ReportPathRegex();
}
