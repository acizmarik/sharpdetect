// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal readonly record struct ShadowVariable(
    ShadowVariableState State,
    LockSetIndex LockSetIndex,
    ProcessThreadId? ExclusiveThread)
{
    public static ShadowVariable CreateVirgin() => new()
    {
        State = ShadowVariableState.Virgin,
        LockSetIndex = LockSetIndex.Empty,
        ExclusiveThread = null
    };
    
    public static ShadowVariable CreateExclusive(ProcessThreadId thread, LockSetIndex lockSetIndex) => new()
    {
        State = ShadowVariableState.Exclusive,
        LockSetIndex = lockSetIndex,
        ExclusiveThread = thread
    };
    
    public static ShadowVariable CreateShared(LockSetIndex lockSetIndex) => new()
    {
        State = ShadowVariableState.Shared,
        LockSetIndex = lockSetIndex,
        ExclusiveThread = null
    };
    
    public static ShadowVariable CreateSharedModified(LockSetIndex lockSetIndex) => new()
    {
        State = ShadowVariableState.SharedModified,
        LockSetIndex = lockSetIndex,
        ExclusiveThread = null
    };
}
