// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal readonly record struct ShadowVariable(
    ShadowVariableState State,
    LockSetIndex LockSetIndex,
    ProcessThreadId? ExclusiveThread,
    ProcessThreadId? LastWriteThread)
{
    public static ShadowVariable CreateVirgin() => new()
    {
        State = ShadowVariableState.Virgin,
        LockSetIndex = LockSetIndex.Empty,
        ExclusiveThread = null,
        LastWriteThread = null
    };
    
    public static ShadowVariable CreateExclusive(ProcessThreadId thread, LockSetIndex lockSetIndex, bool isWrite) => new()
    {
        State = ShadowVariableState.Exclusive,
        LockSetIndex = lockSetIndex,
        ExclusiveThread = thread,
        LastWriteThread = isWrite ? thread : null
    };
    
    public static ShadowVariable CreateShared(LockSetIndex lockSetIndex, ProcessThreadId? lastWriteThread) => new()
    {
        State = ShadowVariableState.Shared,
        LockSetIndex = lockSetIndex,
        ExclusiveThread = null,
        LastWriteThread = lastWriteThread
    };
    
    public static ShadowVariable CreateSharedModified(LockSetIndex lockSetIndex, ProcessThreadId lastWriteThread) => new()
    {
        State = ShadowVariableState.SharedModified,
        LockSetIndex = lockSetIndex,
        ExclusiveThread = null,
        LastWriteThread = lastWriteThread
    };
}
