// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpDetect.Worker.Commands;

public sealed class CommandLineArgumentsJsonConverter : JsonConverter<ImmutableArray<string>>
{
    public override bool HandleNull => true;

    public override ImmutableArray<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return ImmutableArray<string>.Empty;

            case JsonTokenType.String:
                return Split(reader.GetString()!);

            case JsonTokenType.StartArray:
            {
                var builder = ImmutableArray.CreateBuilder<string>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException($"Command-line arguments must be strings, but found {reader.TokenType}.");

                    builder.Add(reader.GetString()!);
                }

                return builder.ToImmutable();
            }

            default:
                throw new JsonException($"Command-line arguments must be an array of strings or a string, but found {reader.TokenType}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, ImmutableArray<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        if (!value.IsDefault)
        {
            foreach (var argument in value)
                writer.WriteStringValue(argument);
        }

        writer.WriteEndArray();
    }
    
    public static ImmutableArray<string> Split(string commandLine)
    {
        var arguments = ImmutableArray.CreateBuilder<string>();
        var current = new StringBuilder();
        var hasArgument = false;
        char? quote = null;

        for (var index = 0; index < commandLine.Length; index++)
        {
            var character = commandLine[index];

            if (character == '\\')
            {
                if (index + 1 >= commandLine.Length)
                    throw new JsonException($"Command-line arguments end with an unescaped '\\': {commandLine}");

                current.Append(commandLine[++index]);
                hasArgument = true;
            }
            else if (quote is not null)
            {
                if (character == quote)
                    quote = null;
                else
                    current.Append(character);
            }
            else if (character is '"' or '\'')
            {
                quote = character;
                hasArgument = true;
            }
            else if (char.IsWhiteSpace(character))
            {
                if (hasArgument)
                {
                    arguments.Add(current.ToString());
                    current.Clear();
                    hasArgument = false;
                }
            }
            else
            {
                current.Append(character);
                hasArgument = true;
            }
        }

        if (quote is not null)
            throw new JsonException($"Command-line arguments contain an unterminated {quote} quote: {commandLine}");

        if (hasArgument)
            arguments.Add(current.ToString());

        return arguments.ToImmutable();
    }
}
