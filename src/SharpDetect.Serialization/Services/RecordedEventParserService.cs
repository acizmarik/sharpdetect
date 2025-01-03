// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using MessagePack;
using SharpDetect.Events;
using SharpDetect.Serialization.Descriptors;
using SharpDetect.Serialization.Formatters;

namespace SharpDetect.Serialization.Services;

internal sealed class RecordedEventParserService : IRecordedEventParser
{
    private readonly MessagePackSerializerOptions _serializerOptions;

    public RecordedEventParserService()
    {
        var formatResolver = new CustomFormatResolver();
        _serializerOptions = MessagePackSerializerOptions.Standard
            .WithResolver(formatResolver);
    }

    public RecordedEvent Parse(ReadOnlyMemory<byte> input)
    {
        var dto = MessagePackSerializer.Deserialize<RecordedEventDto>(input, _serializerOptions);
        return dto.Convert();
    }
}
