// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using MessagePack.Resolvers;
using SharpDetect.Core.Events;
using SharpDetect.Core.Serialization;
using SharpDetect.Serialization.Formatters;

namespace SharpDetect.Serialization.Services;

internal sealed class RecordedEventParserService : IRecordedEventParser
{
    private readonly MessagePackSerializerOptions _serializerOptions;

    public RecordedEventParserService()
    {
        var resolver = CompositeResolver.Create(
            CustomFormatResolver.Instance,
            StandardResolver.Instance
        );
        
        _serializerOptions = MessagePackSerializerOptions.Standard
            .WithResolver(resolver);
    }

    public RecordedEvent Parse(ReadOnlyMemory<byte> input)
    {
        return MessagePackSerializer.Deserialize<RecordedEvent>(input, _serializerOptions);
    }
}
