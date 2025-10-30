// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using SharpDetect.Core.Events;

namespace SharpDetect.Core.Commands;

[MessagePackObject]
public sealed record ProfilerCommand(
    [property: Key(0)] RecordedCommandMetadata Metadata,
    [property: Key(1)] IProfilerCommandArgs CommandArgs);