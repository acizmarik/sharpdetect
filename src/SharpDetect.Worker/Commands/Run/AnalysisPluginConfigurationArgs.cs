// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace SharpDetect.Worker.Commands.Run;

public record AnalysisPluginConfigurationArgs(
    string Path, 
    string FullTypeName,
    string Configuration,
    bool RenderReport = false,
    LogLevel LogLevel = LogLevel.Warning,
    string? TemporaryFilesFolder = null,
    string? ReportsFolder = null,
    string? ReportFileName = null);
