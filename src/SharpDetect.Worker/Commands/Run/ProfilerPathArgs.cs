// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public record ProfilerPathArgs(
    string WindowsX64,
    string LinuxX64)
{
    public string WindowsX64 { get; init; } = PathUtils.NormalizeDirectorySeparators(WindowsX64) ?? WindowsX64;
    public string LinuxX64 { get; init; } = PathUtils.NormalizeDirectorySeparators(LinuxX64) ?? LinuxX64;
}
