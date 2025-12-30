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
        EnsureCorrectProcess(processThreadId.ProcessId);
        if (Owner is { } ownerThreadId && processThreadId != ownerThreadId)
        {
            throw new InvalidOperationException($"Lock {LockObjectId.ObjectId.Value} can not be acquired by {processThreadId.ThreadId.Value} " +
                $"because it is already acquired by {ownerThreadId.ThreadId.Value}");
        }

        Owner = processThreadId;
        LocksCount++;
    }
    
    public void AcquireMultiple(ProcessThreadId processThreadId, int count)
    {
        EnsureCorrectProcess(processThreadId.ProcessId);
        if (Owner is { } ownerThreadId && processThreadId != ownerThreadId)
        {
            throw new InvalidOperationException($"Lock {LockObjectId.ObjectId.Value} can not be acquired by {processThreadId.ThreadId.Value} " + 
                $"because it is already acquired by {ownerThreadId.ThreadId.Value}");
        }

        Owner = processThreadId;
        LocksCount += count;
    }

    public void Release(ProcessThreadId processThreadId)
    {
        EnsureCorrectProcess(processThreadId.ProcessId);
        if (Owner is { } ownerThreadId && processThreadId != ownerThreadId)
        {
            throw new InvalidOperationException($"Lock {LockObjectId.ObjectId.Value} can not be released by {processThreadId.ThreadId.Value} " +
                $"because it is acquired by {ownerThreadId.ThreadId.Value}");
        }

        if (--LocksCount == 0)
            Owner = null;
    }

    public int ReleaseAll(ProcessThreadId processThreadId)
    {
        EnsureCorrectProcess(processThreadId.ProcessId);
        if (Owner is { } ownerThreadId && processThreadId != ownerThreadId)
        {
            throw new InvalidOperationException($"Lock {LockObjectId.ObjectId.Value} can not be released by {processThreadId.ThreadId.Value} " +
                $"because it is acquired by {ownerThreadId.ThreadId.Value}");
        }

        var previousLocksCount = LocksCount;
        LocksCount = 0;
        Owner = null;
        return previousLocksCount;
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
}
