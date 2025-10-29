// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;

namespace SharpDetect.Core.Events;

[MessagePackObject]
public sealed record RecordedEvent(
    [property: Key(0)] RecordedEventMetadata Metadata,
    [property: Key(1)] IRecordedEventArgs EventArgs);
