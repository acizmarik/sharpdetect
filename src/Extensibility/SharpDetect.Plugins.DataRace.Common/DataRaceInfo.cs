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
    ModuleId ModuleId,
    MdMethodDef MethodToken,
    AccessType AccessType,
    DateTime Timestamp);

public sealed record DataRaceInfo(
    uint ProcessId,
    FieldId FieldId,
    ProcessTrackedObjectId? ObjectId,
    AccessInfo CurrentAccess,
    AccessInfo LastAccess,
    DateTime Timestamp);

