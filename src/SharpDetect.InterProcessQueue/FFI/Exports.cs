// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace SharpDetect.InterProcessQueue.FFI;

internal static unsafe class Exports
{
    private static readonly ConcurrentDictionary<nint, Producer> _producers = new();
    private static int _lastProducerId;

    [UnmanagedCallersOnly(EntryPoint = "ipq_producer_create")]
    public static nint CreateProducer(nint queueNamePtr, nint fileNamePtr, int size)
    {
        var clrQueueName = Marshal.PtrToStringAnsi(queueNamePtr);
        var clrFileName = Marshal.PtrToStringAnsi(fileNamePtr);
        if (clrQueueName == null)
            return IntPtr.Zero;

        return CreateProducerImpl(clrQueueName, clrFileName, size);
    }

    internal static nint CreateProducerImpl(string queueName, string? fileName, int size)
    {
        try
        {
            var id = Interlocked.Increment(ref _lastProducerId);
            var producer = new Producer(new Configuration.ProducerMemoryMappedQueueOptions(queueName, fileName, size));
            _producers.TryAdd(id, producer);
            return id;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_producer_destroy")]
    public static void DestroyProducer(nint producerHandle)
    {
        DestroyProducerImpl(producerHandle);
    }

    internal static void DestroyProducerImpl(nint producerHandle)
    {
        if (!_producers.TryGetValue(producerHandle, out var producer))
            return;

        producer.Dispose();
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_producer_enqueue")]
    public static EnqueueErrorType Enqueue(nint producerHandle, byte* data, int size)
    {
        return EnqueueImpl(producerHandle, data, size);
    }

    internal static EnqueueErrorType EnqueueImpl(nint producerHandle, byte* data, int size)
    {
        if (!_producers.TryGetValue(producerHandle, out var producer))
            return EnqueueErrorType.Unavailable;

        var status = producer.Enqueue(new ReadOnlySpan<byte>(data, size));
        if (status.IsSuccess)
            return EnqueueErrorType.OK;

        return status.Error;
    }
}
