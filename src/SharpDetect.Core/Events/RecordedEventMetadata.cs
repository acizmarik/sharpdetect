// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Events;

[MessagePackObject]
public readonly record struct RecordedEventMetadata(
    [property: Key(0)] uint Pid,
    [property: Key(1)] ThreadId Tid);
