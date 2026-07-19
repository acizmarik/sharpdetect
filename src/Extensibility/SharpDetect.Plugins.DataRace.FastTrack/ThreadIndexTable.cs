// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class ThreadIndexTable
{
    private const int InitialCapacity = 8;
    private readonly Dictionary<ProcessThreadId, int> _indices = [];
    private ProcessThreadId[] _threads = new ProcessThreadId[InitialCapacity];

    public int Count { get; private set; }

    public int GetOrAdd(ProcessThreadId threadId)
    {
        if (_indices.TryGetValue(threadId, out var index))
            return index;

        index = Count;
        if (index == _threads.Length)
            Array.Resize(ref _threads, _threads.Length * 2);

        _threads[index] = threadId;
        _indices[threadId] = index;
        Count = index + 1;
        return index;
    }

    public bool TryGetIndex(ProcessThreadId threadId, out int index)
        => _indices.TryGetValue(threadId, out index);

    public ProcessThreadId GetThread(int index)
        => _threads[index];
}
