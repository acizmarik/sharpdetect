// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using PerfWorkload;

var iterations = WorkloadOptions.Parse(args, "--iterations", defaultValue: 1_000_000);
var threadCount = WorkloadOptions.Parse(args, "--threads", defaultValue: 8);

var workers = new Worker[threadCount];
var threads = new Thread[threadCount];
for (var i = 0; i < threadCount; i++)
{
    workers[i] = new Worker(iterations);
    threads[i] = new Thread(workers[i].Run);
}

foreach (var thread in threads)
    thread.Start();
foreach (var thread in threads)
    thread.Join();

var checksum = workers.Sum(static w => w.Checksum)
    + SharedState.LockGuardedCounter
    + SharedState.SemaphoreGuardedCounter;
Console.WriteLine($"PerfWorkload completed: threads={threadCount}, iterations={iterations}, checksum={checksum}.");

internal static class SharedState
{
    public static long LockGuardedCounter;
    public static long SemaphoreGuardedCounter;
}

internal sealed class Worker
{
    private static readonly Lock SharedLock = new();
    private static readonly SemaphoreSlim SharedSemaphore = new(initialCount: 1, maxCount: 1);

    private readonly int _iterations;
    private int _first;
    private int _second;

    public Worker(int iterations)
    {
        _iterations = iterations;
    }

    public long Checksum { get; private set; }

    public void Run()
    {
        for (var i = 0; i < _iterations; i++)
            RunIteration(i);
    }

    private void RunIteration(int index)
    {
        WriteLocalFields(index);
        Checksum += ReadLocalFields();
        IncrementLockGuardedCounter();
        IncrementSemaphoreGuardedCounter();

        if ((index & 0xFF) == 0)
            RunOnThreadPool();
    }

    private void WriteLocalFields(int value)
    {
        _first = value;
        _second = value + 1;
    }

    private long ReadLocalFields()
    {
        return _first + _second;
    }

    private static void IncrementLockGuardedCounter()
    {
        lock (SharedLock)
        {
            SharedState.LockGuardedCounter++;
        }
    }

    private static void IncrementSemaphoreGuardedCounter()
    {
        SharedSemaphore.Wait();
        try
        {
            SharedState.SemaphoreGuardedCounter++;
        }
        finally
        {
            SharedSemaphore.Release();
        }
    }

    private void RunOnThreadPool()
    {
        var task = Task.Run(static () => 1L);
        Checksum += task.Result;
    }
}
