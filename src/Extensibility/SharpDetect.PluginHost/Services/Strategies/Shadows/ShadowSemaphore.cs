// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowSemaphore
{
    // FIXME: add instrumentation for semaphore ctor to capture properly its capacity.
    // Null until the first acquire is observed; treated as authoritative there.
    public int? Count { get; private set; }
    private readonly Queue<ProcessThreadId> _waiters = [];

    public bool TryAcquire(ProcessThreadId tid)
    {
        if (Count is null)
        {
            Count = 0;
            return true;
        }
        
        if (Count > 0)
        {
            Count--;
            return true;
        }
        
        _waiters.Enqueue(tid);
        return false;
    }

    public ProcessThreadId? Release()
    {
        Count = (Count ?? 0) + 1;
        return _waiters.Count > 0 ? _waiters.Dequeue() : null;
    }
}
