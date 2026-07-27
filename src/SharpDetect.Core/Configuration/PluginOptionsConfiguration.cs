// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpDetect.Core.Configuration;

public class PluginOptionsConfiguration
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    
    /// <summary>
    /// Plugin-specific configuration as a JSON object or primitive value.
    /// This will be deserialized by the plugin into its specific configuration type.
    /// </summary>
    public object? RawJsonConfiguration { get; init; }

    public TConfig ParseConfigurationOrDefault<TConfig>()
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
            throw new ArgumentException($"Could not parse the \"Configuration\" section of the analysis plugin: {ex.Message}", ex);
        }
    }
}
