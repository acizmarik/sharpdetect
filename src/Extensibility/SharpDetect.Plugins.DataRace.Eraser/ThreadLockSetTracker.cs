// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class ThreadLockSetTracker(LockSetTable lockSetTable)
{
    private readonly Dictionary<ProcessThreadId, LockSetIndex> _threadLockSets = [];

    public void RegisterThread(ProcessThreadId threadId)
    {
        _threadLockSets[threadId] = LockSetIndex.Empty;
    }

    public void UnregisterThread(ProcessThreadId threadId)
    {
        _threadLockSets.Remove(threadId);
    }
    
    public LockSetIndex GetLockSet(ProcessThreadId threadId)
    {
        return _threadLockSets.GetValueOrDefault(threadId, LockSetIndex.Empty);
    }
    
    public void AcquireLock(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        var currentLockSet = GetLockSet(threadId);
        var newLockSet = lockSetTable.Add(currentLockSet, lockId);
        _threadLockSets[threadId] = newLockSet;
    }

    public void ReleaseLock(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        var currentLockSet = GetLockSet(threadId);
        var newLockSet = lockSetTable.Remove(currentLockSet, lockId);
        _threadLockSets[threadId] = newLockSet;
    }
}
