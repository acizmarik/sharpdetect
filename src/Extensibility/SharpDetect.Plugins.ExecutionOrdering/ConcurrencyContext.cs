// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Plugins.ExecutionOrdering;

public sealed class ConcurrencyContext
{
    private readonly Dictionary<ProcessThreadId, ShadowLock?> _waitingForLocks = [];
    private readonly Dictionary<ProcessThreadId, HashSet<ShadowLock>> _takenLocks = [];
    private readonly Dictionary<ProcessThreadId, ProcessThreadId?> _waitingForThreads = [];

    public bool HasThread(ProcessThreadId id) => _waitingForLocks.ContainsKey(id);
    public bool HasLock(ProcessThreadId id, ShadowLock lockObj) => _takenLocks[id].Contains(lockObj);

    public bool TryGetWaitingLock(ProcessThreadId id, [NotNullWhen(true)] out ShadowLock? lockObj)
    {
        return _waitingForLocks.TryGetValue(id, out lockObj) && lockObj is not null;
    }
    
    public bool TryGetWaitingThread(ProcessThreadId id, [NotNullWhen(true)] out ProcessThreadId? processThreadId)
    {
        return _waitingForThreads.TryGetValue(id, out processThreadId) && processThreadId is not null;
    }
    
    public ShadowLock GetWaitingLock(ProcessThreadId id)
    {
        if (!_waitingForLocks.TryGetValue(id, out var lockObj) || lockObj == null)
            throw new InvalidOperationException($"Thread {id} is not waiting for any lock.");
        
        return lockObj;
    }
    
    public IEnumerable<ShadowLock> GetTakenLocks(ProcessThreadId id)
    {
        return _takenLocks[id];
    }
    
    public void RecordLockAcquireCalled(ProcessThreadId id, ShadowLock lockObj)
    {
        _waitingForLocks[id] = lockObj;
    }

    public void RecordLockAcquireReturned(ProcessThreadId id, ShadowLock lockObj, bool success)
    {
        if (success)
            _takenLocks[id].Add(lockObj);
        _waitingForLocks[id] = null;
    }

    public void RecordLockReleaseReturned(ProcessThreadId id, ShadowLock lockObj)
    {
        _takenLocks[id].Remove(lockObj);
    }
    
    public void RecordObjectWaitCalled(ProcessThreadId id, ShadowLock lockObj)
    {
        _takenLocks[id].Remove(lockObj);
    }
    
    public void RecordObjectWaitReturned(ProcessThreadId id, ShadowLock lockObj)
    {
        _takenLocks[id].Add(lockObj);
    }
    
    public void RecordThreadCreated(ProcessThreadId id)
    {
        _waitingForLocks[id] = null;
        _takenLocks[id] = new HashSet<ShadowLock>();
        _waitingForThreads[id] = null;
    }
    
    public void RecordThreadDestroyed(ProcessThreadId id)
    {
        _waitingForLocks.Remove(id);
        _takenLocks.Remove(id);
        _waitingForThreads.Remove(id);
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