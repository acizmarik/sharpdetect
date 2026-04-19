// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using SharpDetect.InterProcessQueue.Synchronization;

namespace SharpDetect.InterProcessQueue.FFI;

internal static unsafe class Exports
{
    private static readonly ConcurrentDictionary<nint, Producer> _producers = new();
    private static readonly ConcurrentDictionary<nint, Consumer> _consumers = new();
    private static int _lastProducerId;
    private static int _lastConsumerId;

    [UnmanagedCallersOnly(EntryPoint = "ipq_producer_create")]
    public static nint CreateProducer(nint queueNamePtr, nint fileNamePtr, nint semaphoreNamePtr, int size)
    {
        var clrQueueName = Marshal.PtrToStringAnsi(queueNamePtr);
        var clrFileName = Marshal.PtrToStringAnsi(fileNamePtr);
        var clrSemaphoreName = Marshal.PtrToStringAnsi(semaphoreNamePtr);
        if (clrQueueName == null || clrSemaphoreName == null)
            return IntPtr.Zero;

        return CreateProducerImpl(clrQueueName, clrFileName, clrSemaphoreName, size);
    }

    internal static nint CreateProducerImpl(string queueName, string? fileName, string semaphoreName, int size)
    {
        try
        {
            var id = Interlocked.Increment(ref _lastProducerId);
            var configuration = new Configuration.ProducerMemoryMappedQueueOptions(queueName, fileName, size, semaphoreName);
            var semaphore = InterProcessSemaphore.CreateOrOpen(semaphoreName, isOwner: false);
            var producer = new Producer(configuration, semaphore);
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

        var status = producer.TryEnqueue(new ReadOnlySpan<byte>(data, size));
        if (status.IsSuccess)
            return EnqueueErrorType.OK;

        return status.Error;
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_consumer_create")]
    public static nint CreateConsumer(nint queueNamePtr, nint fileNamePtr, nint semaphoreNamePtr, int size)
    {
        var clrQueueName = Marshal.PtrToStringAnsi(queueNamePtr);
        var clrFileName = Marshal.PtrToStringAnsi(fileNamePtr);
        var clrSemaphoreName = Marshal.PtrToStringAnsi(semaphoreNamePtr);
        if (clrQueueName == null || clrSemaphoreName == null)
            return IntPtr.Zero;

        return CreateConsumerImpl(clrQueueName, clrFileName, clrSemaphoreName, size);
    }

    internal static nint CreateConsumerImpl(string queueName, string? fileName, string semaphoreName, int size)
    {
        try
        {
            var id = Interlocked.Increment(ref _lastConsumerId);
            var configuration = new Configuration.ConsumerMemoryMappedQueueOptions(queueName, fileName, size, semaphoreName);
            var semaphore = InterProcessSemaphore.CreateOrOpen(semaphoreName, isOwner: false);
            var consumer = new Consumer(configuration, semaphore);
            _consumers.TryAdd(id, consumer);
            return id;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_consumer_destroy")]
    public static void DestroyConsumer(nint consumerHandle)
    {
        DestroyConsumerImpl(consumerHandle);
    }

    internal static void DestroyConsumerImpl(nint consumerHandle)
    {
        if (!_consumers.TryGetValue(consumerHandle, out var consumer))
            return;

        consumer.Dispose();
        _consumers.TryRemove(consumerHandle, out _);
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_consumer_dequeue")]
    public static DequeueErrorType Dequeue(nint consumerHandle, byte** dataPtr, int* sizePtr)
    {
        return DequeueImpl(consumerHandle, dataPtr, sizePtr);
    }

    internal static DequeueErrorType DequeueImpl(nint consumerHandle, byte** dataPtr, int* sizePtr)
    {
        if (!_consumers.TryGetValue(consumerHandle, out var consumer))
            return DequeueErrorType.UnableToAcquireReadLock;

        var result = consumer.TryDequeue();
        if (!result.IsSuccess)
            return result.Error;

        try
        {
            var memory = result.Value;
            var localMemory = memory.GetLocalMemory();
            
            // Allocate unmanaged memory and copy data
            var size = localMemory.Length;
            var unmanagedPtr = (byte*)Marshal.AllocHGlobal(size);
            localMemory.Span.CopyTo(new Span<byte>(unmanagedPtr, size));
            
            *dataPtr = unmanagedPtr;
            *sizePtr = size;

            // Dispose if it's BorrowedMemory to return to pool
            if (memory is IDisposable disposable)
                disposable.Dispose();

            return DequeueErrorType.OK;
        }
        catch
        {
            return DequeueErrorType.UnableToAcquireReadLock;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_free_memory")]
    public static void FreeMemory(byte* dataPtr)
    {
        FreeMemoryImpl(dataPtr);
    }

    internal static void FreeMemoryImpl(byte* dataPtr)
    {
        if (dataPtr != null)
            Marshal.FreeHGlobal((nint)dataPtr);
    }
}
