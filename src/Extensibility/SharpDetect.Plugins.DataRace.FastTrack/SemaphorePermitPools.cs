// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class SemaphorePermitPools(ThreadClockTable threadClocks) : ITrackedObjectState
{
    private readonly Dictionary<ProcessTrackedObjectId, PermitPool> _pools = [];

    // Permits that exist from the start carry no happens-before edge - they are only counted.
    public void Create(ProcessTrackedObjectId semaphoreId, int initialCount)
    {
        _pools[semaphoreId] = new PermitPool { InitialPermits = Math.Max(initialCount, 0) };
    }

    // Each released permit carries its own copy of the releasing thread's clock.
    public void Release(ProcessTrackedObjectId semaphoreId, ProcessThreadId threadId, int releaseCount)
    {
        var threadVc = threadClocks.GetOrCreateSeen(threadId);
        var pool = GetOrCreate(semaphoreId);
        var released = new VectorClock[Math.Max(releaseCount, 0)];
        for (var i = 0; i < released.Length; i++)
        {
            released[i] = threadVc.Clone();
            pool.ReleasedPermits.Enqueue(released[i]);
        }

        pool.LastReleases[threadId] = released;
        threadVc.Increment(threadId);
    }

    // A release that threw never released its permits; drop the ones nobody has acquired yet
    public void Retract(ProcessTrackedObjectId semaphoreId, ProcessThreadId threadId, int releaseCount)
    {
        if (!_pools.TryGetValue(semaphoreId, out var pool) ||
            !pool.LastReleases.Remove(threadId, out var released) ||
            released.Length == 0)
        {
            return;
        }

        var retracted = new HashSet<VectorClock>(released.Take(releaseCount), ReferenceEqualityComparer.Instance);
        var remaining = pool.ReleasedPermits.Where(permit => !retracted.Contains(permit)).ToArray();
        pool.ReleasedPermits.Clear();
        foreach (var permit in remaining)
            pool.ReleasedPermits.Enqueue(permit);
    }

    public void Acquire(ProcessTrackedObjectId semaphoreId, ProcessThreadId threadId)
    {
        var pool = GetOrCreate(semaphoreId);
        if (pool.InitialPermits > 0)
        {
            pool.InitialPermits--;
            return;
        }

        if (pool.ReleasedPermits.Count == 0)
            return;

        threadClocks.GetOrCreateSeen(threadId).Join(pool.ReleasedPermits.Dequeue());
    }

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId) => _pools.Remove(objectId);

    private PermitPool GetOrCreate(ProcessTrackedObjectId semaphoreId)
    {
        if (!_pools.TryGetValue(semaphoreId, out var pool))
        {
            pool = new PermitPool();
            _pools[semaphoreId] = pool;
        }

        return pool;
    }

    private sealed class PermitPool
    {
        public int InitialPermits;
        public readonly Queue<VectorClock> ReleasedPermits = new();
        public readonly Dictionary<ProcessThreadId, VectorClock[]> LastReleases = [];
    }
}
