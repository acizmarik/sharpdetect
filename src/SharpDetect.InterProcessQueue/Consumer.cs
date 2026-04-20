// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using OperationResult;
using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Memory;
using SharpDetect.InterProcessQueue.Synchronization;
using System.Buffers;
using static OperationResult.Helpers;

namespace SharpDetect.InterProcessQueue;

public sealed class Consumer : IDisposable
{
    private readonly MemoryMappedQueue _queue;
    private readonly ISemaphore _semaphore;
    private bool _disposed;

    public Consumer(ConsumerMemoryMappedQueueOptions queueOptions, ISemaphore semaphore)
        : this(queueOptions, semaphore, arrayPool: null, TimeProvider.System)
    {

    }

    public Consumer(ConsumerMemoryMappedQueueOptions queueOptions, ISemaphore semaphore, ArrayPool<byte> arrayPool)
        : this(queueOptions, semaphore, arrayPool, TimeProvider.System)
    {

    }

    internal Consumer(
        ConsumerMemoryMappedQueueOptions queueOptions,
        ISemaphore semaphore,
        ArrayPool<byte>? arrayPool,
        TimeProvider timeProvider)
    {
        _queue = new MemoryMappedQueue(queueOptions, arrayPool, timeProvider);
        _semaphore = semaphore;
    }

    public void Clear()
    {
        _queue.Clear();
    }

    public Result<ILocalMemory<byte>, DequeueErrorType> TryDequeue()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_semaphore.TryWait())
            return Error(DequeueErrorType.NothingToRead);

        return _queue.Dequeue();
    }

    public Result<ILocalMemory<byte>, DequeueErrorType> TryDequeue(TimeSpan timeout)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (timeout < TimeSpan.Zero || !_semaphore.Wait(timeout))
            return Error(DequeueErrorType.TimeoutExceeded);

        return _queue.Dequeue();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _queue.Dispose();
        _semaphore.Dispose();
    }
}
