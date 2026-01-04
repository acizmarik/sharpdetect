// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Core.Plugins.Models;

public class ShadowLock(ProcessTrackedObjectId processLockObjectId)
{
    public readonly uint ProcessId = processLockObjectId.ProcessId;
    public readonly ProcessTrackedObjectId LockObjectId = processLockObjectId;
    public ProcessThreadId? Owner { get; private set; }
    public int LocksCount { get; private set; }

    public void Acquire(ProcessThreadId processThreadId)
    {
        EnsureCanAcquire(processThreadId);
        Owner = processThreadId;
        LocksCount++;
    }
    
    public void AcquireMultiple(ProcessThreadId processThreadId, int count)
    {
        EnsureCanAcquire(processThreadId);
        Owner = processThreadId;
        LocksCount += count;
    }

    public void Release(ProcessThreadId processThreadId)
    {
        EnsureCanRelease(processThreadId);
        if (--LocksCount == 0)
            Owner = null;
    }

    public int ReleaseAll(ProcessThreadId processThreadId)
    {
        EnsureCanRelease(processThreadId);
        var previousLocksCount = LocksCount;
        LocksCount = 0;
        Owner = null;
        return previousLocksCount;
    }
    
    private void EnsureCanAcquire(ProcessThreadId processThreadId)
    {
        EnsureCorrectProcess(processThreadId.ProcessId);
        if (Owner is { } ownerThreadId && processThreadId != ownerThreadId)
            ThrowLockAlreadyAcquired(processThreadId);
    }

    private void EnsureCanRelease(ProcessThreadId processThreadId)
    {
        EnsureCorrectProcess(processThreadId.ProcessId);
        if (Owner is null)
            ThrowLockNotTakenException(processThreadId);
        if (Owner is { } ownerThreadId && processThreadId != ownerThreadId)
            ThrowLockReleasedByAnotherThread(processThreadId);
    }
    
    private void EnsureCorrectProcess(uint processId)
    {
        if (processId != ProcessId)
            ThrowInvalidProcess(processId);
    }
    
    [DoesNotReturn]
    private void ThrowInvalidProcess(uint processId)
    {
        throw new InvalidOperationException($"Lock operation attempted on process {processId} but lock belongs to process {ProcessId}");
    }
    
    [DoesNotReturn]
    private void ThrowLockNotTakenException(ProcessThreadId processThreadId)
    {
        var lockObjectId = LockObjectId.ObjectId.Value;
        var threadId = processThreadId.ThreadId.Value;
        throw new InvalidOperationException($"Lock {lockObjectId} cannot be released by thread {threadId} because it is not acquired");
    }
    
    [DoesNotReturn]
    private void ThrowLockReleasedByAnotherThread(ProcessThreadId processThreadId)
    {
        var lockObjectId = LockObjectId.ObjectId.Value;
        var threadId = processThreadId.ThreadId.Value;
        var ownerThreadId = Owner?.ThreadId.Value;
        throw new InvalidOperationException($"Lock {lockObjectId} cannot be released by thread {threadId} because it is acquired by thread {ownerThreadId}");
    }
    
    [DoesNotReturn]
    private void ThrowLockAlreadyAcquired(ProcessThreadId processThreadId)
    {
        var lockObjectId = LockObjectId.ObjectId.Value;
        var threadId = processThreadId.ThreadId.Value;
        var ownerThreadId = Owner?.ThreadId.Value;
        throw new InvalidOperationException($"Lock {lockObjectId} cannot be acquired by thread {threadId} because it is acquired by thread {ownerThreadId}");
    }
}
