// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SharpDetect.Worker.Commands.Run;

public record AnalysisPluginConfigurationArgs(
    object? Configuration = null,
    string? PluginFullTypeName = null,
    string? PluginName = null,
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string Path = "%SHARPDETECT_ROOT%/SharpDetect.Plugins.dll",
    bool RenderReport = false,
    LogLevel LogLevel = LogLevel.Warning,
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string? TemporaryFilesFolder = null,
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string? ReportsFolder = null,
    string? ReportFileName = null);
