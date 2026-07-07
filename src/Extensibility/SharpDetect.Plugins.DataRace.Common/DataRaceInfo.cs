// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Common;

public enum AccessType
{
    Read,
    Write
}

public sealed record AccessInfo(
    ProcessThreadId ProcessThreadId,
    string? ThreadName,
    uint MethodOffset,
    AccessType AccessType,
    CapturedStackTrace Stack);

public readonly record struct AccessRecord(
    ProcessThreadId ProcessThreadId,
    uint MethodOffset,
    AccessType AccessType,
    CapturedStackTrace Stack);

public sealed record DataRaceInfo(
    uint ProcessId,
    FieldId FieldId,
    ProcessTrackedObjectId? ObjectId,
    AccessInfo CurrentAccess,
    AccessInfo LastAccess,
    DateTime Timestamp);

