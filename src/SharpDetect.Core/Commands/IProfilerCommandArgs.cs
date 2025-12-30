// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;

namespace SharpDetect.Core.Commands;

[Union((int)ProfilerCommandType.CreateStackSnapshot, typeof(CreateStackTraceSnapshotCommand))]
[Union((int)ProfilerCommandType.CreateStackSnapshots, typeof(CreateStackTraceSnapshotsCommand))]
public interface IProfilerCommandArgs
{
    
}