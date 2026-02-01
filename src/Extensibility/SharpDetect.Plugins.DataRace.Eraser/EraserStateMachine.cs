// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class EraserStateMachine(LockSetTable lockSetTable)
{
    public readonly record struct TransitionResult(
        ShadowVariable NewShadow,
        bool IsRaceDetected,
        ShadowVariableState PreviousState,
        ShadowVariableState NewState,
        LockSetIndex ResultingLockSet);
    
    public TransitionResult ComputeTransition(
        ProcessThreadId threadId,
        ShadowVariable shadow,
        LockSetIndex threadLockSet,
        bool isWrite)
    {
        return shadow.State switch
        {
            ShadowVariableState.Virgin => HandleVirginState(threadId, threadLockSet, isWrite),
            ShadowVariableState.Exclusive when shadow.ExclusiveThread == threadId => HandleSameThreadExclusiveAccess(threadId, shadow, isWrite),
            ShadowVariableState.Exclusive => HandleDifferentThreadExclusiveAccess(threadId, shadow, threadLockSet, isWrite),
            ShadowVariableState.Shared or ShadowVariableState.SharedModified => HandleSharedStateAccess(threadId, shadow, threadLockSet, isWrite),
            _ => throw new InvalidOperationException($"Unknown shadow variable state: {shadow.State}")
        };
    }

    private static TransitionResult HandleVirginState(ProcessThreadId threadId, LockSetIndex threadLockSet, bool isWrite)
    {
        var newShadow = ShadowVariable.CreateExclusive(threadId, threadLockSet, isWrite);
        return new TransitionResult(
            NewShadow: newShadow,
            IsRaceDetected: false,
            PreviousState: ShadowVariableState.Virgin,
            NewState: ShadowVariableState.Exclusive,
            ResultingLockSet: threadLockSet);
    }

    private static TransitionResult HandleSameThreadExclusiveAccess(ProcessThreadId threadId, ShadowVariable shadow, bool isWrite)
    {
        var newShadow = isWrite ? shadow with { LastWriteThread = threadId } : shadow;
        return new TransitionResult(
            NewShadow: newShadow,
            IsRaceDetected: false,
            PreviousState: ShadowVariableState.Exclusive,
            NewState: ShadowVariableState.Exclusive,
            ResultingLockSet: shadow.LockSetIndex);
    }

    private TransitionResult HandleDifferentThreadExclusiveAccess(
        ProcessThreadId threadId,
        ShadowVariable shadow,
        LockSetIndex threadLockSet,
        bool isWrite)
    {
        var newLockSet = lockSetTable.Intersect(shadow.LockSetIndex, threadLockSet);
        var newState = isWrite ? ShadowVariableState.SharedModified : ShadowVariableState.Shared;
        
        var lastWriteThread = isWrite ? threadId : shadow.LastWriteThread;
        var newShadow = newState == ShadowVariableState.SharedModified
            ? ShadowVariable.CreateSharedModified(newLockSet, lastWriteThread!.Value)
            : ShadowVariable.CreateShared(newLockSet, lastWriteThread);
        
        var isRaceDetected = newState == ShadowVariableState.SharedModified && newLockSet.IsEmpty;
        return new TransitionResult(
            NewShadow: newShadow,
            isRaceDetected,
            PreviousState: ShadowVariableState.Exclusive,
            NewState: newState,
            ResultingLockSet: newLockSet);
    }

    private TransitionResult HandleSharedStateAccess(
        ProcessThreadId threadId,
        ShadowVariable shadow,
        LockSetIndex threadLockSet,
        bool isWrite)
    {
        var newLockSet = lockSetTable.Intersect(shadow.LockSetIndex, threadLockSet);
        var previousState = shadow.State;
        var newState = isWrite ? ShadowVariableState.SharedModified : shadow.State;
        
        var lastWriteThread = isWrite ? threadId : shadow.LastWriteThread;
        var newShadow = newState == ShadowVariableState.SharedModified
            ? ShadowVariable.CreateSharedModified(newLockSet, lastWriteThread!.Value)
            : ShadowVariable.CreateShared(newLockSet, lastWriteThread);

        var isRaceDetected = newState == ShadowVariableState.SharedModified 
            && newLockSet.IsEmpty 
            && shadow.LastWriteThread != null
            && shadow.LastWriteThread != threadId;
        
        return new TransitionResult(
            NewShadow: newShadow,
            isRaceDetected,
            PreviousState: previousState,
            NewState: newState,
            ResultingLockSet: newLockSet);
    }
}
