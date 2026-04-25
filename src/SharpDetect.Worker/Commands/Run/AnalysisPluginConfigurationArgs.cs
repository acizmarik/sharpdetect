// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public sealed class AnalysisPluginConfigurationArgs
{
    public const string DefaultPath = "%SHARPDETECT_ROOT%/SharpDetect.Plugins.dll";
    public const LogLevel DefaultLogLevel = LogLevel.Warning;
    public const bool DefaultRenderReport = false;
    
    public object? Configuration { get; }
    public string? PluginFullTypeName { get; }
    public string? PluginName { get; }
    [JsonIgnore] public string Path { get; }
    public bool RenderReport { get; }
    public LogLevel LogLevel { get; }
    public string? TemporaryFilesFolder { get; }
    public string? ReportsFolder { get; }
    public string? ReportFileName { get; }
    public string? SessionId { get; }

    [JsonConstructor]
    public AnalysisPluginConfigurationArgs(
        object? configuration = null,
        string? pluginFullTypeName = null,
        string? pluginName = null,
        string path = DefaultPath,
        bool renderReport = DefaultRenderReport,
        LogLevel logLevel = DefaultLogLevel,
        string? temporaryFilesFolder = null,
        string? reportsFolder = null,
        string? reportFileName = null,
        string? sessionId = null)
    {
        Guard.IsNotNullOrWhiteSpace(path);
        if (string.IsNullOrWhiteSpace(pluginFullTypeName) && string.IsNullOrWhiteSpace(pluginName))
            throw new ArgumentException($"Either {nameof(pluginFullTypeName)} or {nameof(pluginName)} must be provided.");
        
        Configuration = configuration;
        PluginFullTypeName = pluginFullTypeName;
        PluginName = pluginName;
        Path = EnvironmentUtils.ExpandEnvironmentVariablesForPath(path);
        RenderReport = renderReport;
        LogLevel = logLevel;
        TemporaryFilesFolder = temporaryFilesFolder;
        ReportsFolder = reportsFolder;
        ReportFileName = reportFileName;
        SessionId = sessionId;
    }
    
    [JsonPropertyName(nameof(Path))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? PathSerialized => Path == DefaultPath ? null : Path;
}
