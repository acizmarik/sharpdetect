// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public record HostConfigurationArgs(
    string Path, 
    string? Args,
    KeyValuePair<string, string>[]? AdditionalEnvironmentVariables)
{
    public string Path { get; init; } = PathUtils.NormalizeDirectorySeparators(Path) ?? Path;
}
