// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SharpDetect.Core.Configuration;

public class PluginOptionsConfiguration
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
    
    /// <summary>
    /// Plugin-specific configuration as a JSON object or primitive value.
    /// This will be deserialized by the plugin into its specific configuration type.
    /// </summary>
    public object? RawJsonConfiguration { get; init; }
    
    public TConfig ParseConfigurationOrDefault<TConfig>(ILogger logger)
        where TConfig : IPluginOptionsConfig<TConfig>
    {
        if (RawJsonConfiguration == null)
            return TConfig.Default;

        try
        {
            if (RawJsonConfiguration is string jsonString && !string.IsNullOrWhiteSpace(jsonString))
                return JsonSerializer.Deserialize<TConfig>(jsonString, _jsonSerializerOptions) ?? TConfig.Default;

            var json = JsonSerializer.Serialize(RawJsonConfiguration, _jsonSerializerOptions);
            return JsonSerializer.Deserialize<TConfig>(json, _jsonSerializerOptions) ?? TConfig.Default;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse configuration JSON, using default settings");
            return TConfig.Default;
        }
    }
}
