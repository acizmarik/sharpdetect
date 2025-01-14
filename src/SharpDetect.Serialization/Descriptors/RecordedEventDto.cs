// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using SharpDetect.Core.Events;

namespace SharpDetect.Serialization.Descriptors;

[MessagePackObject]
public sealed record RecordedEventDto(
    [property: Key(0)] RecordedEventMetadataDto Metadata,
    [property: Key(1)] IRecordedEventArgsDto EventArgs)
{
    public RecordedEvent Convert()
    {
        return new RecordedEvent(
            Metadata: Metadata.Convert(),
            EventArgs: EventArgs.Convert());
    }
}
