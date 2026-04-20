// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using OperationResult;
using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Synchronization;
using static OperationResult.Helpers;

namespace SharpDetect.InterProcessQueue;

public sealed class Producer : IDisposable
{
    private readonly MemoryMappedQueue _queue;
    private readonly ISemaphore _semaphore;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    public Producer(ProducerMemoryMappedQueueOptions queueOptions, ISemaphore semaphore)
        : this(queueOptions, semaphore, TimeProvider.System)
    {

    }

    internal Producer(MemoryMappedQueueOptions queueOptions, ISemaphore semaphore, TimeProvider timeProvider)
    {
        _queue = new MemoryMappedQueue(queueOptions, null, timeProvider);
        _semaphore = semaphore;
        _timeProvider = timeProvider;
    }

    public Status<EnqueueErrorType> TryEnqueue(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = _queue.Enqueue(data);
        if (result.IsSuccess)
            _semaphore.Release();
        return result;
    }

    public Status<EnqueueErrorType> TryEnqueue(ReadOnlySpan<byte> data, TimeSpan timeout)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (timeout < TimeSpan.Zero)
            return Error(EnqueueErrorType.TimeoutExceeded);

        if (timeout == TimeSpan.Zero)
            return TryEnqueue(data);

        var endTimeStamp = _timeProvider.GetUtcNow() + timeout;
        while (_timeProvider.GetUtcNow() < endTimeStamp)
        {
            var result = _queue.Enqueue(data);
            if (result.IsSuccess)
            {
                _semaphore.Release();
                return Ok();
            }

            Thread.Yield();
        }

        return Error(EnqueueErrorType.TimeoutExceeded);
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
