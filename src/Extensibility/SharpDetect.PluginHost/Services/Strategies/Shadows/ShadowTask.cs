// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowTask
{
    public bool Completed { get; private set; }
    private readonly Queue<ProcessThreadId> _waiters = [];

    public bool RegisterWaiter(ProcessThreadId tid)
    {
        if (Completed)
            return false;
        
        _waiters.Enqueue(tid);
        return true;
    }

    public IReadOnlyCollection<ProcessThreadId> Complete()
    {
        Completed = true;
        return _waiters.Count == 0 ? [] : _waiters;
    }
}
