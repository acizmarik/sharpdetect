// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using SharpDetect.Core.Events;
using SharpDetect.Core.Serialization;
using SharpDetect.Serialization.Formatters;

namespace SharpDetect.Serialization.Services;

internal sealed class RecordedEventParserService : IRecordedEventParser
{
    private readonly MessagePackSerializerOptions _serializerOptions;
    private readonly RecordedEventFormatter _formatter;

    public RecordedEventParserService()
    {
        _serializerOptions = MessagePackSerializerOptions.Standard
            .WithResolver(CompositeFormatResolver.Instance);
        _formatter = new RecordedEventFormatter(_serializerOptions.Resolver);
    }

    public RecordedEvent Parse(ReadOnlyMemory<byte> input)
    {
        var reader = new MessagePackReader(input);
        return _formatter.Deserialize(ref reader, _serializerOptions);
    }
}
