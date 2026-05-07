// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.Deadlock;

public sealed class ConcurrencyContext
{
    private readonly Dictionary<ProcessThreadId, ProcessTrackedObjectId?> _waitingForLocks = [];
    private readonly Dictionary<ProcessThreadId, HashSet<ProcessTrackedObjectId>> _takenLocks = [];
    private readonly Dictionary<ProcessThreadId, ProcessThreadId?> _waitingForThreads = [];
    private readonly Dictionary<ProcessTrackedObjectId, ProcessThreadId> _lockOwners = [];

    public bool HasThread(ProcessThreadId id) => _waitingForLocks.ContainsKey(id);

    public bool TryGetWaitingLock(ProcessThreadId id, [NotNullWhen(true)] out ProcessTrackedObjectId? lockId)
    {
        return _waitingForLocks.TryGetValue(id, out lockId) && lockId is not null;
    }
    
    public bool TryGetLockOwner(ProcessTrackedObjectId lockId, out ProcessThreadId owner)
    {
        return _lockOwners.TryGetValue(lockId, out owner);
    }
    
    public bool TryGetWaitingThread(ProcessThreadId id, [NotNullWhen(true)] out ProcessThreadId? processThreadId)
    {
        return _waitingForThreads.TryGetValue(id, out processThreadId) && processThreadId is not null;
    }
    
    public void RecordLockAcquireCalled(ProcessThreadId id, ProcessTrackedObjectId lockId)
    {
        _waitingForLocks[id] = lockId;
    }

    public void RecordLockAcquireReturned(ProcessThreadId id, ProcessTrackedObjectId lockId, bool success)
    {
        if (success)
        {
            _takenLocks[id].Add(lockId);
            _lockOwners[lockId] = id;
        }
        _waitingForLocks[id] = null;
    }

    public void RecordLockReleaseReturned(ProcessThreadId id, ProcessTrackedObjectId lockId)
    {
        _takenLocks[id].Remove(lockId);
        _lockOwners.Remove(lockId);
    }
    
    public void RecordObjectWaitCalled(ProcessThreadId id, ProcessTrackedObjectId lockId)
    {
        _takenLocks[id].Remove(lockId);
        _lockOwners.Remove(lockId);
    }
    
    public void RecordObjectWaitReturned(ProcessThreadId id, ProcessTrackedObjectId lockId)
    {
        _takenLocks[id].Add(lockId);
        _lockOwners[lockId] = id;
    }
    
    public void RecordThreadCreated(ProcessThreadId id)
    {
        _waitingForLocks[id] = null;
        _takenLocks[id] = [];
        _waitingForThreads[id] = null;
    }
    
    public void RecordThreadJoinCalled(ProcessThreadId id, ProcessThreadId processJoiningThreadId)
    {
        _waitingForThreads[id] = processJoiningThreadId;
    }
    
    public void RecordThreadJoinReturned(ProcessThreadId id)
    {
        _waitingForThreads[id] = null;
    }
}
