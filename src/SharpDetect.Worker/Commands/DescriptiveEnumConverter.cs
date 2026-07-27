// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpDetect.Worker.Commands;

internal sealed class DescriptiveEnumConverterFactory : JsonConverterFactory
{
    private static readonly JsonStringEnumConverter StringEnumConverter = new();

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerConverter = StringEnumConverter.CreateConverter(typeToConvert, options);
        var converterType = typeof(DescriptiveEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType, innerConverter)!;
    }
}

internal sealed class DescriptiveEnumConverter<TEnum>(JsonConverter<TEnum> innerConverter) : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var rawValue = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

        try
        {
            return innerConverter.Read(ref reader, typeToConvert, options);
        }
        catch (JsonException)
        {
            throw new JsonException($"\"{rawValue}\" is not a valid value. Valid values are: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        => innerConverter.Write(writer, value, options);
}
