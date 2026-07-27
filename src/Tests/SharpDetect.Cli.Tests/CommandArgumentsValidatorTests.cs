// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class CommandArgumentsValidatorTests
{
    private static readonly string ExistingAssembly = typeof(CommandArgumentsValidatorTests).Assembly.Location;
    private static readonly string ExistingDirectory = Path.GetDirectoryName(ExistingAssembly)!;
    
    private static string Validate(RunCommandArgs arguments)
    {
        var exception = Record.Exception(() => CommandArgumentsValidator.ValidateRunCommandArguments(arguments));
        return exception?.Message ?? string.Empty;
    }

    private static RunCommandArgs BuildArguments(
        TargetConfigurationArgs? target = null,
        AnalysisPluginConfigurationArgs? analysis = null,
        RuntimeConfigurationArgs? runtime = null)
    {
        return new RunCommandArgs(
            Runtime: runtime,
            Target: target ?? new TargetConfigurationArgs(path: ExistingAssembly),
            Analysis: analysis ?? new AnalysisPluginConfigurationArgs(pluginName: "FastTrack"));
    }

    [Fact]
    public void Validate_MissingTargetSection_NamesTheSection()
    {
        var message = Validate(new RunCommandArgs(
            Runtime: null,
            Target: null!,
            Analysis: new AnalysisPluginConfigurationArgs(pluginName: "FastTrack")));

        Assert.Contains("missing the required section \"Target\"", message);
    }

    [Fact]
    public void Validate_MissingAnalysisSection_NamesTheSection()
    {
        var message = Validate(new RunCommandArgs(
            Runtime: null,
            Target: new TargetConfigurationArgs(path: ExistingAssembly),
            Analysis: null!));

        Assert.Contains("missing the required section \"Analysis\"", message);
    }

    [Fact]
    public void Validate_NonExistentWorkingDirectory_NamesTheDirectory()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"sharpdetect-missing-{Guid.NewGuid():N}");
        var message = Validate(BuildArguments(
            target: new TargetConfigurationArgs(path: ExistingAssembly, workingDirectory: workingDirectory)));

        Assert.Contains("Could not find target working directory", message);
        Assert.Contains(Path.GetFileName(workingDirectory), message);
    }

    [Fact]
    public void Validate_ExistingWorkingDirectory_IsAccepted()
    {
        var message = Validate(BuildArguments(
            target: new TargetConfigurationArgs(path: ExistingAssembly, workingDirectory: ExistingDirectory)));

        Assert.DoesNotContain("working directory", message);
    }

    [Fact]
    public void Validate_NonExistentTargetAssembly_NamesTheAssembly()
    {
        var message = Validate(BuildArguments(
            target: new TargetConfigurationArgs(path: "definitely-not-here.dll")));

        Assert.Contains("Could not find target assembly", message);
        Assert.Contains("definitely-not-here.dll", message);
    }

    [Fact]
    public void Validate_TestAssemblyKindWithoutTestSection_IsRejected()
    {
        var message = Validate(BuildArguments(
            target: new TargetConfigurationArgs(path: ExistingAssembly, kind: TargetKind.TestAssembly)));

        Assert.Contains("\"Test\" must be specified", message);
    }

    [Fact]
    public void Validate_TestSectionWithExecutableKind_IsRejected()
    {
        var message = Validate(BuildArguments(
            target: new TargetConfigurationArgs(
                path: ExistingAssembly,
                kind: TargetKind.Executable,
                test: new TestTargetConfigurationArgs(runner: TestRunner.Mtp))));

        Assert.Contains("\"Test\" is only applicable", message);
    }

    [Fact]
    public void Validate_NonExistentStandardInputRedirectionFile_IsRejected()
    {
        var message = Validate(BuildArguments(
            target: new TargetConfigurationArgs(
                path: ExistingAssembly,
                redirectInputOutput: new RedirectInputOutputConfigurationArgs(
                    stdinFilePath: Path.Combine(ExistingDirectory, "no-such-input.txt")))));

        Assert.Contains("Could not find standard input redirection file", message);
    }

    [Fact]
    public void Validate_StandardOutputRedirectionIntoMissingDirectory_IsRejected()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), $"sharpdetect-missing-{Guid.NewGuid():N}");
        var message = Validate(BuildArguments(
            target: new TargetConfigurationArgs(
                path: ExistingAssembly,
                redirectInputOutput: new RedirectInputOutputConfigurationArgs(
                    stdoutFilePath: Path.Combine(missingDirectory, "stdout.txt")))));

        Assert.Contains("standard output redirection file", message);
    }

    [Fact]
    public void Validate_RedirectionFilePathsInSingleConsoleMode_AreNotChecked()
    {
        var message = Validate(BuildArguments(
            target: new TargetConfigurationArgs(
                path: ExistingAssembly,
                redirectInputOutput: new RedirectInputOutputConfigurationArgs(
                    singleConsoleMode: true,
                    stdinFilePath: Path.Combine(ExistingDirectory, "no-such-input.txt")))));

        Assert.DoesNotContain("redirection file", message);
    }

    [Fact]
    public void Validate_NonExistentTemporaryFilesFolder_IsRejected()
    {
        var temporaryFilesFolder = Path.Combine(Path.GetTempPath(), $"sharpdetect-missing-{Guid.NewGuid():N}");
        var message = Validate(BuildArguments(
            analysis: new AnalysisPluginConfigurationArgs(
                pluginName: "FastTrack",
                temporaryFilesFolder: temporaryFilesFolder)));

        Assert.Contains("Could not find temporary files folder", message);
    }

    [Fact]
    public void Validate_BareHostCommandName_IsNotCheckedForExistence()
    {
        var message = Validate(BuildArguments(
            runtime: new RuntimeConfigurationArgs(
                Host: new HostConfigurationArgs(path: HostConfigurationArgs.DefaultHost),
                Profiler: null)));

        Assert.DoesNotContain("host executable", message);
    }

    [Fact]
    public void Validate_NonExistentHostExecutablePath_IsRejected()
    {
        var message = Validate(BuildArguments(
            runtime: new RuntimeConfigurationArgs(
                Host: new HostConfigurationArgs(path: Path.Combine(ExistingDirectory, "no-such-host")),
                Profiler: null)));

        Assert.Contains("Could not find host executable", message);
    }

    [Fact]
    public void Validate_SeveralProblems_AreAllReportedAtOnce()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), $"sharpdetect-missing-{Guid.NewGuid():N}");
        var message = Validate(BuildArguments(
            target: new TargetConfigurationArgs(
                path: "definitely-not-here.dll",
                workingDirectory: missingDirectory,
                kind: TargetKind.TestAssembly)));

        Assert.Contains("Could not find target assembly", message);
        Assert.Contains("Could not find target working directory", message);
        Assert.Contains("\"Test\" must be specified", message);
    }
}
