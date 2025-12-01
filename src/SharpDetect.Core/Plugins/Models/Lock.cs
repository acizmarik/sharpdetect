// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Core.Plugins.Models;

public class Lock(ProcessTrackedObjectId processLockObjectId)
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
