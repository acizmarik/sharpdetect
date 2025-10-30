// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;

namespace SharpDetect.Core.Events;

[MessagePackObject]
public readonly record struct RecordedCommandMetadata([property: Key(0)] uint Pid);