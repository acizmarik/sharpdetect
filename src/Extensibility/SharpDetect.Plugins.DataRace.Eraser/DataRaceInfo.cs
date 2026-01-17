// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed record DataRaceInfo(
    uint ProcessId,
    FieldId FieldId,
    AccessInfo CurrentAccess,
    AccessInfo? LastAccess,
    ShadowVariableState PreviousState,
    ShadowVariableState NewState,
    LockSetIndex CandidateLockSet,
    DateTime Timestamp);

internal enum AccessType
{
    Read,
    Write
}

internal sealed record AccessInfo(
    ProcessThreadId ProcessThreadId,
    string? ThreadName,
    ModuleId ModuleId,
    MdMethodDef MethodToken,
    AccessType AccessType,
    DateTime Timestamp);