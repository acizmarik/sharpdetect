// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Plugins.Models;

public class Lock(TrackedObjectId lockObjectId)
{
    public readonly TrackedObjectId LockObjectId = lockObjectId;
    public ThreadId? Owner { get; private set; }
    public int LocksCount { get; private set; }

    public void Acquire(ThreadId threadId)
    {
        if (Owner is ThreadId ownerThreadId && threadId != ownerThreadId)
        {
            throw new InvalidOperationException($"Lock {LockObjectId.Value} can not be acquired by {threadId.Value} " +
                $"because it is already acquired by {ownerThreadId.Value}");
        }

        Owner = threadId;
        LocksCount++;
    }

    public void Release(ThreadId threadId)
    {
        if (Owner is ThreadId ownerThreadId && threadId != ownerThreadId)
        {
            throw new InvalidOperationException($"Lock {LockObjectId.Value} can not be released by {threadId.Value} " +
                $"because it is acquired by {ownerThreadId.Value}");
        }

        if (--LocksCount == 0)
            Owner = null;
    }
}
