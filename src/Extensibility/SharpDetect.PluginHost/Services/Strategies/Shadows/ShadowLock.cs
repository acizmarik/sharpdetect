// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowLock
{
    public ProcessThreadId? Owner { get; private set; }
    public int ReentrantCount { get; private set; }
    private readonly Queue<ProcessThreadId> _waiters = [];

    public bool TryAcquire(ProcessThreadId tid)
    {
        if (Owner is null)
        {
            Owner = tid;
            ReentrantCount = 1;
            return true;
        }
        
        if (Owner == tid)
        {
            ReentrantCount++;
            return true;
        }
        
        _waiters.Enqueue(tid);
        return false;
    }

    public ProcessThreadId? Release(ProcessThreadId tid)
    {
        if (Owner != tid)
            return null;
        
        if (--ReentrantCount > 0)
            return null;
        
        Owner = null;
        ReentrantCount = 0;
        return DequeueWaiterOrNull();
    }

    public bool TryReleaseForWait(
        ProcessThreadId tid,
        out ProcessThreadId? nextWaiter,
        [NotNullWhen(true)] out int? suspendedCount)
    {
        if (Owner != tid)
        {
            // Wait without owning lock - causes exception
            suspendedCount = null;
            nextWaiter = null;
            return false;
        }
        
        suspendedCount = ReentrantCount;
        nextWaiter = DequeueWaiterOrNull();
        Owner = null;
        ReentrantCount = 0;
        return true;
    }

    public bool TryReacquireAfterWait(ProcessThreadId tid, int suspendedCount)
    {
        if (Owner is null)
        {
            Owner = tid;
            ReentrantCount = suspendedCount;
            return true;
        }
        
        _waiters.Enqueue(tid);
        return false;
    }

    public ProcessThreadId? AbandonByDestroy(ProcessThreadId tid)
    {
        if (Owner != tid)
            return null;
        
        Owner = null;
        ReentrantCount = 0;
        return DequeueWaiterOrNull();
    }

    private ProcessThreadId? DequeueWaiterOrNull()
        => _waiters.Count > 0 ? _waiters.Dequeue() : null;
}
