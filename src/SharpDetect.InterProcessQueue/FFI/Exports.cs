// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using SharpDetect.InterProcessQueue.Memory;
using SharpDetect.InterProcessQueue.Synchronization;

namespace SharpDetect.InterProcessQueue.FFI;

internal static unsafe class Exports
{
    private static readonly ConcurrentDictionary<nint, Producer> Producers = new();
    private static readonly ConcurrentDictionary<nint, Consumer> Consumers = new();
    private static int _lastProducerId;
    private static int _lastConsumerId;

    [UnmanagedCallersOnly(EntryPoint = "ipq_producer_create")]
    public static nint CreateProducer(nint queueNamePtr, nint fileNamePtr, nint semaphoreNamePtr, int size)
    {
        try
        {
            return TryMarshalQueueNames(queueNamePtr, fileNamePtr, semaphoreNamePtr, out var queueName, out var fileName, out var semaphoreName)
                ? CreateProducerImpl(queueName, fileName, semaphoreName, size)
                : IntPtr.Zero;
        }
        catch (Exception ex)
        {
            ReportUnexpectedError(nameof(CreateProducer), ex);
            return IntPtr.Zero;
        }
    }

    internal static nint CreateProducerImpl(string queueName, string? fileName, string semaphoreName, int size)
    {
        var id = Interlocked.Increment(ref _lastProducerId);
        var configuration = new Configuration.ProducerMemoryMappedQueueOptions(queueName, fileName, size, semaphoreName);
        var semaphore = InterProcessSemaphore.CreateOrOpen(semaphoreName, isOwner: false);
        var producer = new Producer(configuration, semaphore);
        Producers.TryAdd(id, producer);
        return id;
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_producer_destroy")]
    public static void DestroyProducer(nint producerHandle)
    {
        try
        {
            DestroyProducerImpl(producerHandle);
        }
        catch (Exception ex)
        {
            ReportUnexpectedError(nameof(DestroyProducer), ex);
        }
    }

    internal static void DestroyProducerImpl(nint producerHandle)
    {
        if (Producers.TryRemove(producerHandle, out var producer))
            producer.Dispose();
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_producer_enqueue")]
    public static EnqueueErrorType Enqueue(nint producerHandle, byte* data, int size)
    {
        try
        {
            return EnqueueImpl(producerHandle, data, size);
        }
        catch (Exception ex)
        {
            ReportUnexpectedError(nameof(Enqueue), ex);
            return EnqueueErrorType.InternalError;
        }
    }

    internal static EnqueueErrorType EnqueueImpl(nint producerHandle, byte* data, int size)
    {
        if (!Producers.TryGetValue(producerHandle, out var producer))
            return EnqueueErrorType.Unavailable;

        try
        {
            var status = producer.TryEnqueue(new ReadOnlySpan<byte>(data, size));
            return status.IsSuccess ? EnqueueErrorType.OK : status.Error;
        }
        catch (ObjectDisposedException)
        {
            return EnqueueErrorType.Unavailable;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_consumer_create")]
    public static nint CreateConsumer(nint queueNamePtr, nint fileNamePtr, nint semaphoreNamePtr, int size)
    {
        try
        {
            return TryMarshalQueueNames(queueNamePtr, fileNamePtr, semaphoreNamePtr, out var queueName, out var fileName, out var semaphoreName)
                ? CreateConsumerImpl(queueName, fileName, semaphoreName, size)
                : IntPtr.Zero;
        }
        catch (Exception ex)
        {
            ReportUnexpectedError(nameof(CreateConsumer), ex);
            return IntPtr.Zero;
        }
    }

    internal static nint CreateConsumerImpl(string queueName, string? fileName, string semaphoreName, int size)
    {
        var id = Interlocked.Increment(ref _lastConsumerId);
        var configuration = new Configuration.ConsumerMemoryMappedQueueOptions(queueName, fileName, size, semaphoreName);
        var semaphore = InterProcessSemaphore.CreateOrOpen(semaphoreName, isOwner: false);
        var consumer = new Consumer(configuration, semaphore);
        Consumers.TryAdd(id, consumer);
        return id;
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_consumer_destroy")]
    public static void DestroyConsumer(nint consumerHandle)
    {
        try
        {
            DestroyConsumerImpl(consumerHandle);
        }
        catch (Exception ex)
        {
            ReportUnexpectedError(nameof(DestroyConsumer), ex);
        }
    }

    internal static void DestroyConsumerImpl(nint consumerHandle)
    {
        if (Consumers.TryRemove(consumerHandle, out var consumer))
            consumer.Dispose();
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_consumer_dequeue")]
    public static DequeueErrorType Dequeue(nint consumerHandle, byte** dataPtr, int* sizePtr)
    {
        try
        {
            return DequeueImpl(consumerHandle, dataPtr, sizePtr);
        }
        catch (Exception ex)
        {
            ReportUnexpectedError(nameof(Dequeue), ex);
            return DequeueErrorType.InternalError;
        }
    }

    internal static DequeueErrorType DequeueImpl(nint consumerHandle, byte** dataPtr, int* sizePtr)
    {
        if (!Consumers.TryGetValue(consumerHandle, out var consumer))
            return DequeueErrorType.Unavailable;

        try
        {
            var result = consumer.TryDequeue();
            return result.IsSuccess ? CopyToUnmanaged(result.Value, dataPtr, sizePtr) : result.Error;
        }
        catch (ObjectDisposedException)
        {
            return DequeueErrorType.Unavailable;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_consumer_dequeue_timeout")]
    public static DequeueErrorType DequeueWithTimeout(nint consumerHandle, byte** dataPtr, int* sizePtr, int timeoutMs)
    {
        try
        {
            return DequeueWithTimeoutImpl(consumerHandle, dataPtr, sizePtr, timeoutMs);
        }
        catch (Exception ex)
        {
            ReportUnexpectedError(nameof(DequeueWithTimeout), ex);
            return DequeueErrorType.InternalError;
        }
    }

    internal static DequeueErrorType DequeueWithTimeoutImpl(nint consumerHandle, byte** dataPtr, int* sizePtr, int timeoutMs)
    {
        if (!Consumers.TryGetValue(consumerHandle, out var consumer))
            return DequeueErrorType.Unavailable;

        try
        {
            var result = consumer.TryDequeue(TimeSpan.FromMilliseconds(timeoutMs));
            return result.IsSuccess ? CopyToUnmanaged(result.Value, dataPtr, sizePtr) : result.Error;
        }
        catch (ObjectDisposedException)
        {
            return DequeueErrorType.Unavailable;
        }
    }

    private static DequeueErrorType CopyToUnmanaged(QueueMessage message, byte** dataPtr, int* sizePtr)
    {
        using (message)
        {
            var messageMemory = message.Memory;

            // Allocate unmanaged memory and copy data
            var size = messageMemory.Length;
            var unmanagedPtr = (byte*)Marshal.AllocHGlobal(size);
            messageMemory.Span.CopyTo(new Span<byte>(unmanagedPtr, size));

            *dataPtr = unmanagedPtr;
            *sizePtr = size;
        }

        return DequeueErrorType.OK;
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_free_memory")]
    public static void FreeMemory(byte* dataPtr)
    {
        try
        {
            FreeMemoryImpl(dataPtr);
        }
        catch (Exception ex)
        {
            ReportUnexpectedError(nameof(FreeMemory), ex);
        }
    }

    internal static void FreeMemoryImpl(byte* dataPtr)
    {
        if (dataPtr != null)
            Marshal.FreeHGlobal((nint)dataPtr);
    }

    [UnmanagedCallersOnly(EntryPoint = "ipq_register_process")]
    public static int RegisterProcess(nint queueNamePtr, nint fileNamePtr, int size, int pid)
    {
        try
        {
            var clrQueueName = Marshal.PtrToStringAnsi(queueNamePtr);
            var clrFileName = Marshal.PtrToStringAnsi(fileNamePtr);
            if (clrQueueName == null || pid <= 0)
                return 1;

            return RegisterProcessImpl(clrQueueName, clrFileName, size, pid);
        }
        catch (Exception ex)
        {
            ReportUnexpectedError(nameof(RegisterProcess), ex);
            return 1;
        }
    }

    private static int RegisterProcessImpl(string queueName, string? fileName, int size, int pid)
    {
        using var table = new RegistrationTable(queueName, fileName, size, createAsConsumer: false);
        table.Register((uint)pid);
        return 0;
    }

    private static bool TryMarshalQueueNames(
        nint queueNamePtr,
        nint fileNamePtr,
        nint semaphoreNamePtr,
        [NotNullWhen(true)] out string? queueName,
        out string? fileName,
        [NotNullWhen(true)] out string? semaphoreName)
    {
        queueName = Marshal.PtrToStringAnsi(queueNamePtr);
        fileName = Marshal.PtrToStringAnsi(fileNamePtr);
        semaphoreName = Marshal.PtrToStringAnsi(semaphoreNamePtr);
        return queueName != null && semaphoreName != null;
    }

    private static void ReportUnexpectedError(string operation, Exception exception)
    {
        try
        {
            Console.Error.WriteLine($"[SharpDetect.InterProcessQueue] Unexpected error in {operation}: {exception}");
        }
        catch
        {
            // Reporting must never throw back into the FFI boundary
        }
    }
}
