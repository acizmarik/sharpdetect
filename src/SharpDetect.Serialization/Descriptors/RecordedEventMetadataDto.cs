// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Serialization.Descriptors;

[MessagePackObject]
public readonly record struct RecordedEventMetadataDto(
    [property: Key(0)] uint Pid,
    [property: Key(1)] nuint Tid)
{
    public readonly RecordedEventMetadata Convert()
    {
        return new RecordedEventMetadata(Pid, new ThreadId(Tid));
    }
}
