// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpDetect.Worker.Commands;

public static class CommandDeserializer
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = null
    };
    
    public static TCommandArgs DeserializeCommandArguments<TCommandArgs>(string configuration)
        where TCommandArgs : class
    {
        try
        {
            var deserialized = JsonSerializer.Deserialize<TCommandArgs>(configuration, _jsonSerializerOptions) 
                   ?? throw new JsonException("Could not parse provided configuration.");
            return ExpandEnvironmentVariables<TCommandArgs>(deserialized);
        }
        catch (Exception e)
        {
            throw new ArgumentException("Error during loading configuration.", e);
        }
    }
    
    private static TCommandArgs ExpandEnvironmentVariables<TCommandArgs>(TCommandArgs commandArgs)
        where TCommandArgs : class
    {
        var serialized = JsonSerializer.Serialize(commandArgs, _jsonSerializerOptions);
        var expanded = Environment.ExpandEnvironmentVariables(serialized);
        return JsonSerializer.Deserialize<TCommandArgs>(expanded, _jsonSerializerOptions) 
               ?? throw new JsonException("Could not expand environment variables in the command arguments.");
    }
}