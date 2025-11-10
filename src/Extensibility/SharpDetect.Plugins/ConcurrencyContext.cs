// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Plugins;

public sealed class ConcurrencyContext
{
    private readonly Dictionary<ProcessThreadId, Lock?> _waitingForLocks = [];
    private readonly Dictionary<ProcessThreadId, HashSet<Lock>> _takenLocks = [];

    public bool HasThread(ProcessThreadId id) => _waitingForLocks.ContainsKey(id);
    public bool HasLock(ProcessThreadId id, Lock lockObj) => _takenLocks[id].Contains(lockObj);

    public bool TryGetWaitingLock(ProcessThreadId id, [NotNullWhen(true)] out Lock? lockObj)
    {
        return _waitingForLocks.TryGetValue(id, out lockObj) && lockObj is not null;
    }
    
    public Lock GetWaitingLock(ProcessThreadId id)
    {
        if (!_waitingForLocks.TryGetValue(id, out var lockObj) || lockObj == null)
            throw new InvalidOperationException($"Thread {id} is not waiting for any lock.");
        
        return lockObj;
    }
    
    public IEnumerable<Lock> GetTakenLocks(ProcessThreadId id)
    {
        return _takenLocks[id];
    }
    
    public void RecordLockAcquireCalled(ProcessThreadId id, Lock lockObj)
    {
        _waitingForLocks[id] = lockObj;
    }

    public void RecordLockAcquireReturned(ProcessThreadId id, Lock lockObj, bool success)
    {
        if (success)
            _takenLocks[id].Add(lockObj);
        _waitingForLocks[id] = null;
    }

    public void RecordLockReleaseReturned(ProcessThreadId id, Lock lockObj)
    {
        _takenLocks[id].Remove(lockObj);
    }
    
    public void RecordObjectWaitCalled(ProcessThreadId id, Lock lockObj)
    {
        _takenLocks[id].Remove(lockObj);
    }
    
    public void RecordObjectWaitReturned(ProcessThreadId id, Lock lockObj)
    {
        _takenLocks[id].Add(lockObj);
    }
    
    public void RecordThreadCreated(ProcessThreadId id)
    {
        _waitingForLocks[id] = null;
        _takenLocks[id] = new HashSet<Lock>();
    }
    
    public void RecordThreadDestroyed(ProcessThreadId id)
    {
        _waitingForLocks.Remove(id);
        _takenLocks.Remove(id);
    }
}