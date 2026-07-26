// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace SharpDetect.Worker.Commands;

public static class CommandDeserializer
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        Converters = { new DescriptiveEnumConverterFactory() },
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
        catch (JsonException e)
        {
            throw new ArgumentException($"Error during loading configuration: {Describe(e)}", e);
        }
    }

    private static string Describe(JsonException exception)
    {
        var message = exception.Message;

        if (exception.Path is not { Length: > 0 } path || message.Contains(path, StringComparison.Ordinal))
            return message;

        message = $"{message} Path: {path}";
        if (message.Contains("LineNumber", StringComparison.Ordinal))
            return message;

        if (exception.LineNumber is { } lineNumber && exception.BytePositionInLine is { } bytePositionInLine)
            message = $"{message} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}";

        return message;
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
