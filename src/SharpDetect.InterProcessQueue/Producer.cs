// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using OperationResult;
using SharpDetect.InterProcessQueue.Configuration;
using static OperationResult.Helpers;

namespace SharpDetect.InterProcessQueue;

public sealed class Producer : IDisposable
{
    private readonly MemoryMappedQueue _queue;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    public Producer(ProducerMemoryMappedQueueOptions queueOptions)
        : this(queueOptions, TimeProvider.System)
    {

    }

    internal Producer(MemoryMappedQueueOptions queueOptions, TimeProvider timeProvider)
    {
        _queue = new MemoryMappedQueue(queueOptions);
        _timeProvider = timeProvider;
    }

    public Status<EnqueueErrorType> Enqueue(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _queue.Enqueue(data);
    }

    public Status<EnqueueErrorType> Enqueue(ReadOnlySpan<byte> data, TimeSpan timeout)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (timeout < TimeSpan.Zero)
            return Error(EnqueueErrorType.TimeoutExceeded);

        if (timeout == TimeSpan.Zero)
            return Enqueue(data);

        var endTimeStamp = _timeProvider.GetUtcNow() + timeout;
        while (_timeProvider.GetUtcNow() < endTimeStamp)
        {
            var result = _queue.Enqueue(data);
            if (result.IsSuccess)
                return Ok();

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
    }
}
