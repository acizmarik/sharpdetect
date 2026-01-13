// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public record RedirectInputOutputConfigurationArgs(
    bool SingleConsoleMode,
    string? StdinFilePath,
    string? StdoutFilePath,
    string? StderrFilePath)
{
    public string? StdinFilePath { get; init; } = PathUtils.NormalizeDirectorySeparators(StdinFilePath);
    public string? StdoutFilePath { get; init; } = PathUtils.NormalizeDirectorySeparators(StdoutFilePath);
    public string? StderrFilePath { get; init; } = PathUtils.NormalizeDirectorySeparators(StderrFilePath);
}
