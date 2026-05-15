// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
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

    public bool TryDescribeResidualState([NotNullWhen(true)] out string? description)
    {
        if (Completed && _waiters.Count == 0)
        {
            description = null;
            return false;
        }

        description = $"completed={Completed}, owner={OwnerThreadId?.ToString() ?? "none"}, waiters={_waiters.Count}";
        return true;
    }
}
