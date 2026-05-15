// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowSemaphore
{
    public int Count { get; private set; }
    private readonly LinkedList<ProcessThreadId> _waiters = [];
    private readonly Dictionary<ProcessThreadId, int> _outstandingPermits = [];

    public void Initialize(int initialCount) => Count = initialCount;

    public bool TryAcquire(ProcessThreadId tid)
    {
        if (Count > 0)
        {
            Count--;
            _outstandingPermits[tid] = _outstandingPermits.GetValueOrDefault(tid) + 1;
            return true;
        }
        
        _waiters.AddLast(tid);
        return false;
    }

    public ProcessThreadId? Release(ProcessThreadId tid)
    {
        Count++;
        if (_outstandingPermits.TryGetValue(tid, out var held) && held > 0)
        {
            if (held == 1)
                _outstandingPermits.Remove(tid);
            else
                _outstandingPermits[tid] = held - 1;
        }

        if (_waiters.Count == 0)
            return null;

        var result = _waiters.First();
        _waiters.RemoveFirst();
        return result;
    }

    public void RemoveWaiter(ProcessThreadId tid)
    {
        _waiters.Remove(tid);
    }

    public int AbandonPermitsByDestroy(ProcessThreadId tid)
    {
        return _outstandingPermits.Remove(tid, out var held) ? held : 0;
    }
}
