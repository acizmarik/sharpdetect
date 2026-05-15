// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowTask
{
    public bool Completed { get; private set; }
    public ProcessThreadId? OwnerThreadId { get; private set; }
    private LinkedList<ProcessThreadId> _waiters = [];

    public void AttachOwner(ProcessThreadId tid) => OwnerThreadId ??= tid;

    public bool RegisterWaiter(ProcessThreadId tid)
    {
        if (Completed)
            return false;
        
        _waiters.AddLast(tid);
        return true;
    }

    public IReadOnlyCollection<ProcessThreadId> Complete()
    {
        Completed = true;
        if (_waiters.Count == 0)
            return [];

        var result = _waiters;
        _waiters = [];
        return result;
    }

    public void RemoveWaiter(ProcessThreadId tid)
    {
        _waiters.Remove(tid);
    }
}
