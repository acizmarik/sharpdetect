// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using OperationResult;
using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Memory;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static OperationResult.Helpers;

namespace SharpDetect.InterProcessQueue;

/// <summary>
/// A single-producer/single-consumer ring over a shared-memory region
/// </summary>
internal sealed unsafe class MemoryMappedQueue : IDisposable
{
    private readonly MemoryMappedQueueOptions _options;
    private readonly CircularBuffer _buffer;
    private readonly SharedMemoryProvider _sharedMemoryProvider;
    private readonly ArrayPool<byte>? _arrayPool;
    private readonly BorrowedMemory<byte>? _reusableBorrowed;
    private readonly SharedMemory _sharedMemory;
    private readonly long _capacityWithoutHeader;
    private long _cachedReadOffset;
    private long _cachedWriteOffset;
    private bool _disposed;

    public MemoryMappedQueue(MemoryMappedQueueOptions options)
        : this(options, null, TimeProvider.System)
    {

    }

    public MemoryMappedQueue(MemoryMappedQueueOptions options, ArrayPool<byte> arrayPool)
        : this(options, arrayPool, TimeProvider.System)
    {

    }

    internal MemoryMappedQueue(MemoryMappedQueueOptions options, ArrayPool<byte>? arrayPool, TimeProvider timeProvider)
    {
        _options = options;
        var headerSize = Unsafe.SizeOf<QueueHeader>();
        var capacityWithoutHeader = options.Capacity - headerSize;
        var minimumViableSize = headerSize + sizeof(int) + 1;
        if (_options.Capacity < minimumViableSize)
            throw new ArgumentException($"Capacity must be at least {minimumViableSize} bytes (header: {headerSize}, size prefix: {sizeof(int)}, minimum data: 1).", nameof(options));

        _arrayPool = arrayPool;
        _reusableBorrowed = arrayPool != null ? new BorrowedMemory<byte>(arrayPool) : null;
        _capacityWithoutHeader = capacityWithoutHeader;

        _sharedMemoryProvider = _options switch
        {
            ConsumerMemoryMappedQueueOptions => SharedMemoryProvider.CreateForConsumer(options.Name, options.File, options.Capacity),
            ProducerMemoryMappedQueueOptions => SharedMemoryProvider.CreateForProducer(options.Name, options.File, options.Capacity),
            _ => throw new NotSupportedException($"Unsupported queue options type: {_options.GetType().FullName}.")
        };
        _sharedMemory = _sharedMemoryProvider.CreateAccessor();
        InitializeOrValidateHeader();

        var header = GetHeader();
        _cachedReadOffset = Volatile.Read(ref header->ReadOffset);
        _cachedWriteOffset = Volatile.Read(ref header->WriteOffset);
        _buffer = new CircularBuffer(_sharedMemory.GetPointer() + headerSize, capacityWithoutHeader);
    }

    private void InitializeOrValidateHeader()
    {
        var header = (long*)_sharedMemory.GetPointer();
        SharedMemoryHeaderProtocol.InitializeOrValidate(
            header, QueueHeader.ExpectedMagic, _options.Capacity, _options.Name, "SharpDetect IPC queue header");
    }

    public Status<EnqueueErrorType> Enqueue(ReadOnlySpan<byte> data)
    {
        var header = GetHeader();
        var required = 4L + data.Length;
        if (required > _capacityWithoutHeader)
            return Error(EnqueueErrorType.NotEnoughFreeMemory);

        // WriteOffset is owned by this (producer) process; a plain read of our own line suffices.
        var writeOffset = Volatile.Read(ref header->WriteOffset);

        // Re-read the consumer's ReadOffset only when the cached view says we are full.
        if (writeOffset - _cachedReadOffset + required > _capacityWithoutHeader)
        {
            _cachedReadOffset = Volatile.Read(ref header->ReadOffset);
            if (writeOffset - _cachedReadOffset + required > _capacityWithoutHeader)
                return Error(EnqueueErrorType.NotEnoughFreeMemory);
        }

        Span<byte> sizeBuffer = stackalloc byte[4];
        MemoryMarshal.Write(sizeBuffer, data.Length);
        _buffer.Write(sizeBuffer, writeOffset);
        _buffer.Write(data, writeOffset + 4);

        // Release-store: publishes the payload writes above to the consumer's acquire-load.
        Volatile.Write(ref header->WriteOffset, writeOffset + required);
        return Ok();
    }

    public Result<ILocalMemory<byte>, DequeueErrorType> Dequeue()
    {
        var header = GetHeader();

        // ReadOffset is owned by this (consumer) process; a plain read of our own line suffices.
        var readOffset = Volatile.Read(ref header->ReadOffset);

        // Re-read the producer's WriteOffset only when the cached view says we are empty.
        if (readOffset == _cachedWriteOffset)
        {
            // Acquire-load: pairs with the producer's release-store of WriteOffset.
            _cachedWriteOffset = Volatile.Read(ref header->WriteOffset);
            if (readOffset == _cachedWriteOffset)
                return Error(DequeueErrorType.NothingToRead);
        }

        var sizeBuffer = GetArray(size: 4);
        _buffer.Read(readOffset, 4, sizeBuffer);
        var dataSize = MemoryMarshal.Read<int>(sizeBuffer);
        ReturnArray(sizeBuffer);

        var unreadBytes = _cachedWriteOffset - readOffset;
        if (dataSize < 0 || dataSize > _capacityWithoutHeader || 4 + (long)dataSize > unreadBytes)
            ThrowDataCorruptionDetected();

        var resultBuffer = GetArray(size: dataSize);
        _buffer.Read(readOffset + 4, dataSize, resultBuffer);
        ILocalMemory<byte> result;
        if (_reusableBorrowed != null)
        {
            _reusableBorrowed.Reset(resultBuffer, dataSize);
            result = _reusableBorrowed;
        }
        else
        {
            result = new OwnedMemory<byte>(resultBuffer);
        }

        // Release-store: frees the consumed bytes back to the producer.
        Volatile.Write(ref header->ReadOffset, readOffset + 4 + dataSize);
        return Ok(result);
    }

    private QueueHeader* GetHeader()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return (QueueHeader*)_sharedMemory.GetPointer();
    }

    private byte[] GetArray(int size)
    {
        return _arrayPool?.Rent(size) ?? new byte[size];
    }

    private void ReturnArray(byte[] array)
    {
        _arrayPool?.Return(array);
    }

    [DoesNotReturn]
    private static void ThrowDataCorruptionDetected()
    {
        throw new QueueMemoryCorruptionException();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _sharedMemory.Dispose();
        _sharedMemoryProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
