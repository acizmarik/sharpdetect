// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowSemaphore
{
    public int Count { get; private set; }
    private readonly Queue<ProcessThreadId> _waiters = [];

    public void Initialize(int initialCount) => Count = initialCount;

    public bool TryAcquire(ProcessThreadId tid)
    {
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
        Count++;
        return _waiters.Count > 0 ? _waiters.Dequeue() : null;
    }
}
