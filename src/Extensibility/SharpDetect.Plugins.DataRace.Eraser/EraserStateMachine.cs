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
            ShadowVariableState.Virgin => HandleVirginState(threadId, threadLockSet),
            ShadowVariableState.Exclusive when shadow.ExclusiveThread == threadId => HandleSameThreadExclusiveAccess(shadow),
            ShadowVariableState.Exclusive => HandleDifferentThreadExclusiveAccess(shadow, threadLockSet, isWrite),
            ShadowVariableState.Shared or ShadowVariableState.SharedModified => HandleSharedStateAccess(shadow, threadLockSet, isWrite),
            _ => throw new InvalidOperationException($"Unknown shadow variable state: {shadow.State}")
        };
    }

    private static TransitionResult HandleVirginState(ProcessThreadId threadId, LockSetIndex threadLockSet)
    {
        var newShadow = ShadowVariable.CreateExclusive(threadId, threadLockSet);
        return new TransitionResult(
            NewShadow: newShadow,
            IsRaceDetected: false,
            PreviousState: ShadowVariableState.Virgin,
            NewState: ShadowVariableState.Exclusive,
            ResultingLockSet: threadLockSet);
    }

    private static TransitionResult HandleSameThreadExclusiveAccess(ShadowVariable shadow)
    {
        return new TransitionResult(
            NewShadow: shadow,
            IsRaceDetected: false,
            PreviousState: ShadowVariableState.Exclusive,
            NewState: ShadowVariableState.Exclusive,
            ResultingLockSet: shadow.LockSetIndex);
    }

    private TransitionResult HandleDifferentThreadExclusiveAccess(
        ShadowVariable shadow,
        LockSetIndex threadLockSet,
        bool isWrite)
    {
        var newLockSet = lockSetTable.Intersect(shadow.LockSetIndex, threadLockSet);
        var newState = isWrite ? ShadowVariableState.SharedModified : ShadowVariableState.Shared;
        
        var newShadow = newState == ShadowVariableState.SharedModified
            ? ShadowVariable.CreateSharedModified(newLockSet)
            : ShadowVariable.CreateShared(newLockSet);

        return new TransitionResult(
            NewShadow: newShadow,
            IsRaceDetected: newLockSet.IsEmpty,
            PreviousState: ShadowVariableState.Exclusive,
            NewState: newState,
            ResultingLockSet: newLockSet);
    }

    private TransitionResult HandleSharedStateAccess(
        ShadowVariable shadow,
        LockSetIndex threadLockSet,
        bool isWrite)
    {
        var newLockSet = lockSetTable.Intersect(shadow.LockSetIndex, threadLockSet);
        var previousState = shadow.State;
        var newState = isWrite ? ShadowVariableState.SharedModified : shadow.State;
        
        var newShadow = newState == ShadowVariableState.SharedModified
            ? ShadowVariable.CreateSharedModified(newLockSet)
            : ShadowVariable.CreateShared(newLockSet);

        return new TransitionResult(
            NewShadow: newShadow,
            IsRaceDetected: newLockSet.IsEmpty,
            PreviousState: previousState,
            NewState: newState,
            ResultingLockSet: newLockSet);
    }
}
