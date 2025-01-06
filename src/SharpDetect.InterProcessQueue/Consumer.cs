// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using OperationResult;
using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Memory;
using System.Buffers;
using static OperationResult.Helpers;

namespace SharpDetect.InterProcessQueue;

public sealed class Consumer : IDisposable
{
    private readonly MemoryMappedQueue _queue;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    public Consumer(ConsumerMemoryMappedQueueOptions queueOptions)
        : this(queueOptions, arrayPool: null, TimeProvider.System)
    {

    }

    public Consumer(ConsumerMemoryMappedQueueOptions queueOptions, ArrayPool<byte> arrayPool)
        : this(queueOptions, arrayPool, TimeProvider.System)
    {

    }

    internal Consumer(ConsumerMemoryMappedQueueOptions queueOptions, ArrayPool<byte>? arrayPool, TimeProvider timeProvider)
    {
        _queue = arrayPool != null
            ? new MemoryMappedQueue(queueOptions, arrayPool)
            : new MemoryMappedQueue(queueOptions);
        _timeProvider = timeProvider;
    }

    public Result<ILocalMemory<byte>, DequeueErrorType> Dequeue()
    {
        return _queue.Dequeue();
    }

    public Result<ILocalMemory<byte>, DequeueErrorType> Dequeue(TimeSpan timeout)
    {
        var endTimeStamp = _timeProvider.GetUtcNow() + timeout;
        while (_timeProvider.GetUtcNow() < endTimeStamp)
        {
            var result = _queue.Dequeue();
            if (result.IsSuccess)
                return Ok(result.Value);

            Thread.Yield();
        }

        return Error(DequeueErrorType.TimeoutExceeded);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _queue.Dispose();
    }
}
