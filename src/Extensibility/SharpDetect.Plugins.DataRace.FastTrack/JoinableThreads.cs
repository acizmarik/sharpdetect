// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class JoinableThreads(ThreadClockTable threadClocks) : ITrackedObjectState
{
    private readonly Dictionary<ProcessThreadId, ProcessTrackedObjectId> _runningThreadObjects = [];
    private readonly Dictionary<ProcessTrackedObjectId, ProcessThreadId> _exitedThreadsByObject = [];

    public void RecordStarted(ProcessTrackedObjectId threadObjectId, ProcessThreadId threadId)
        => _runningThreadObjects[threadId] = threadObjectId;

    public void RecordExited(ProcessThreadId threadId)
    {
        if (_runningThreadObjects.Remove(threadId, out var threadObjectId))
            _exitedThreadsByObject[threadObjectId] = threadId;
    }

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId)
    {
        if (_exitedThreadsByObject.Remove(objectId, out var threadId))
            threadClocks.Remove(threadId);
    }
}
