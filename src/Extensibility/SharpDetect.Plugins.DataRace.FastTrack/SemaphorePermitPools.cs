// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class SemaphorePermitPools(ThreadClockTable threadClocks) : ITrackedObjectState
{
    private readonly Dictionary<ProcessTrackedObjectId, Queue<VectorClock>> _pools = [];

    // Permits that exist from the start carry no happens-before edge
    public void Create(ProcessTrackedObjectId semaphoreId, int initialCount)
    {
        var pool = new Queue<VectorClock>(capacity: initialCount);
        for (var i = 0; i < initialCount; i++)
            pool.Enqueue(threadClocks.CreateClock());

        _pools[semaphoreId] = pool;
    }

    // Each released permit carries its own copy of the releasing thread's clock
    public void Release(ProcessTrackedObjectId semaphoreId, ProcessThreadId threadId, int releaseCount)
    {
        var threadVc = threadClocks.GetOrCreate(threadId);
        var pool = GetOrCreate(semaphoreId);
        for (var i = 0; i < releaseCount; i++)
            pool.Enqueue(threadVc.Clone());

        threadVc.Increment(threadId);
    }

    public void Acquire(ProcessTrackedObjectId semaphoreId, ProcessThreadId threadId)
    {
        var pool = GetOrCreate(semaphoreId);
        if (pool.Count == 0)
            return;

        threadClocks.GetOrCreate(threadId).Join(pool.Dequeue());
    }

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId) => _pools.Remove(objectId);

    private Queue<VectorClock> GetOrCreate(ProcessTrackedObjectId semaphoreId)
    {
        if (!_pools.TryGetValue(semaphoreId, out var pool))
        {
            pool = new Queue<VectorClock>();
            _pools[semaphoreId] = pool;
        }

        return pool;
    }
}
