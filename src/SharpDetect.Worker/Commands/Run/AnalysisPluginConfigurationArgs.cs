// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public record AnalysisPluginConfigurationArgs(
    string Path, 
    string FullTypeName,
    string Configuration,
    bool RenderReport = false,
    LogLevel LogLevel = LogLevel.Warning,
    string? TemporaryFilesFolder = null,
    string? ReportsFolder = null)
{
    public string Path { get; init; } = PathUtils.NormalizeDirectorySeparators(Path) ?? Path;
    public string? TemporaryFilesFolder { get; init; } = PathUtils.NormalizeDirectorySeparators(TemporaryFilesFolder);
    public string? ReportsFolder { get; init; } = PathUtils.NormalizeDirectorySeparators(ReportsFolder);
}
