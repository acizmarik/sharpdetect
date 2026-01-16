// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.ExecutionOrdering;

public sealed class ThreadObjectRegistry
{
    private readonly Dictionary<ProcessTrackedObjectId, ProcessThreadId> _objectIdToThreadIdLookup = [];
    
    public void RegisterMapping(ProcessTrackedObjectId processThreadObjectId, ProcessThreadId processThreadId)
    {
        _objectIdToThreadIdLookup[processThreadObjectId] = processThreadId;
    }
    
    public bool TryGetThreadId(ProcessTrackedObjectId processThreadObjectId, out ProcessThreadId processThreadId)
    {
        return _objectIdToThreadIdLookup.TryGetValue(processThreadObjectId, out processThreadId);
    }
    
    public ProcessThreadId GetThreadId(ProcessTrackedObjectId processThreadObjectId)
    {
        return !_objectIdToThreadIdLookup.TryGetValue(processThreadObjectId, out var processThreadId) 
            ? throw new KeyNotFoundException($"No thread mapping found for object ID {processThreadObjectId.ObjectId.Value}") 
            : processThreadId;
    }
}

