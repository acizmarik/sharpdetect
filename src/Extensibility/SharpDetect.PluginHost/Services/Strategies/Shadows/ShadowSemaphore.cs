// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowSemaphore(int initialCount, int capacity)
{
    public int Capacity { get; } = capacity;
    public int Count { get; private set; } = initialCount;
    private readonly LinkedList<ProcessThreadId> _waiters = [];
    private int _outstandingPermits;

    public bool TryAcquire(ProcessThreadId tid)
    {
        if (Count > 0)
        {
            Count--;
            _outstandingPermits++;
            return true;
        }
        
        _waiters.AddLast(tid);
        return false;
    }

    public IReadOnlyList<ProcessThreadId> Release(ProcessThreadId tid, int count)
    {
        _outstandingPermits -= Math.Min(_outstandingPermits, count);

        var effectiveCount = Math.Min(count, Capacity - Count);
        if (effectiveCount <= 0)
            return [];

        Count += effectiveCount;
        var wakeCount = Math.Min(effectiveCount, _waiters.Count);
        if (wakeCount == 0)
            return [];

        var woken = new ProcessThreadId[wakeCount];
        for (var i = 0; i < wakeCount; i++)
        {
            woken[i] = _waiters.First!.Value;
            _waiters.RemoveFirst();
        }
        return woken;
    }

    public void RemoveWaiter(ProcessThreadId tid)
    {
        _waiters.Remove(tid);
    }

    public IReadOnlyCollection<ProcessThreadId> DrainWaiters()
    {
        if (_waiters.Count == 0)
            return [];

        var result = _waiters.ToArray();
        _waiters.Clear();
        return result;
    }

    public bool TryDescribeResidualState([NotNullWhen(true)] out string? description)
    {
        if (_waiters.Count == 0)
        {
            description = null;
            return false;
        }

        description = $"waiters={_waiters.Count}, outstandingPermits={_outstandingPermits}";
        return true;
    }
}
