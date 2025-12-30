// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Plugins;

internal sealed class LockRegistry
{
    private readonly Dictionary<ProcessTrackedObjectId, ShadowLock> _locks = [];
    
    public ShadowLock GetOrAdd(ProcessTrackedObjectId processLockObjectId)
    {
        if (_locks.TryGetValue(processLockObjectId, out var lockObj))
            return lockObj;

        lockObj = new ShadowLock(processLockObjectId);
        _locks.Add(processLockObjectId, lockObj);
        return lockObj;
    }
    
    public ShadowLock Get(ProcessTrackedObjectId processLockObjectId)
    {
        return !TryGet(processLockObjectId, out var lockObj) 
            ? throw new KeyNotFoundException($"Could not resolve objectId {processLockObjectId.ObjectId.Value} to a known lock.")
            : lockObj;
    }
    
    public bool TryGet(ProcessTrackedObjectId processLockObjectId, out ShadowLock lockObj)
    {
        return _locks.TryGetValue(processLockObjectId, out lockObj!);
    }
    
    public bool Remove(ProcessTrackedObjectId processLockObjectId)
    {
        return _locks.Remove(processLockObjectId);
    }
    
    public int RemoveRange(uint processId, IEnumerable<TrackedObjectId> trackedObjectIds)
    {
        return trackedObjectIds
            .Count(trackedObjectId => Remove(new ProcessTrackedObjectId(processId, trackedObjectId)));
    }

    public IReadOnlySet<ShadowLock> GetAllLocks()
    {
        return _locks.Values.ToHashSet();
    }
}

