// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using SharpDetect.Core.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public record TargetConfigurationArgs(
    string Path,
    Architecture Architecture, 
    string? Args, 
    string? WorkingDirectory, 
    KeyValuePair<string, string>[]? AdditionalEnvironmentVariables,
    RedirectInputOutputConfigurationArgs? RedirectInputOutput)
{
    public string Path { get; init; } = PathUtils.NormalizeDirectorySeparators(Path) ?? Path;
    public string? WorkingDirectory { get; init; } = PathUtils.NormalizeDirectorySeparators(WorkingDirectory);
}
