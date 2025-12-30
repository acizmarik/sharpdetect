// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Commands;

[MessagePackObject]
public sealed record CreateStackTraceSnapshotCommand(
    [property: Key(0)] ThreadId ThreadId) : IProfilerCommandArgs;

[MessagePackObject]
public sealed record CreateStackTraceSnapshotsCommand(
    [property: Key(0)] ThreadId[] ThreadIds) : IProfilerCommandArgs;
