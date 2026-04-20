// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using OperationResult;
using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Memory;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpDetect.InterProcessQueue.Synchronization;
using static OperationResult.Helpers;

namespace SharpDetect.InterProcessQueue;

internal sealed unsafe class MemoryMappedQueue : IDisposable
{
    private readonly MemoryMappedQueueOptions _options;
    private readonly CircularBuffer _buffer;
    private readonly SharedMemoryProvider _sharedMemoryProvider;
    private readonly ArrayPool<byte>? _arrayPool;
    private readonly SharedMemory _sharedMemory;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _maxLockDuration;
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
        _timeProvider = timeProvider;

        _sharedMemoryProvider = _options switch
        {
            ConsumerMemoryMappedQueueOptions => SharedMemoryProvider.CreateForConsumer(options.Name, options.File, options.Capacity),
            ProducerMemoryMappedQueueOptions => SharedMemoryProvider.CreateForProducer(options.Name, options.File, options.Capacity),
            _ => throw new NotSupportedException($"Unsupported queue options type: {_options.GetType().FullName}.")
        };
        _sharedMemory = _sharedMemoryProvider.CreateAccessor();

        _maxLockDuration = TimeSpan.FromSeconds(10);
        _buffer = new CircularBuffer(_sharedMemory.GetPointer() + headerSize, capacityWithoutHeader);
    }

    ~MemoryMappedQueue()
    {
        Dispose();
    }

    internal void Clear()
    {
        var nowTicks = _timeProvider.GetUtcNow().UtcTicks;
        var header = (QueueHeader*)_sharedMemory.GetPointer();
        Volatile.Write(ref header->ReadLockToken, 0);
        Volatile.Write(ref header->WriteLockToken, 0);
        Volatile.Write(ref header->ReadOffset, 0);
        Volatile.Write(ref header->WriteOffset, 0);
        Volatile.Write(ref header->ReadLockAcquiredTimestampTicks, nowTicks);
        Volatile.Write(ref header->WriteLockAcquiredTimestampTicks, nowTicks);
    }

    public Status<EnqueueErrorType> Enqueue(ReadOnlySpan<byte> data)
    {
        var header = GetHeader();
        var token = LockToken.Next();
        var lockAcquired = false;

        try
        {
            lockAcquired = TryAcquireLock(token, ref header->WriteLockToken, ref header->WriteLockAcquiredTimestampTicks);
            if (!lockAcquired)
                return Error(EnqueueErrorType.UnableToAcquireWriteLock);

            if (!HasEnoughSpace(4 + data.Length))
                return Error(EnqueueErrorType.NotEnoughFreeMemory);

            var writeOffset = header->WriteOffset;
            var nextWriteOffset = writeOffset + 4 + data.Length;
            Span<byte> sizeBuffer = stackalloc byte[4];
            MemoryMarshal.Write(sizeBuffer, data.Length);
            _buffer.Write(sizeBuffer, writeOffset);
            _buffer.Write(data, writeOffset + 4);

            if (Interlocked.CompareExchange(ref header->WriteOffset, nextWriteOffset, writeOffset) != writeOffset)
                ThrowDataCorruptionDetected();
        }
        finally
        {
            if (lockAcquired)
            {
                if (Interlocked.CompareExchange(ref header->WriteLockToken, 0, token) != token)
                    ThrowDataCorruptionDetected();
            }
        }

        return Ok();
    }

    public Result<ILocalMemory<byte>, DequeueErrorType> Dequeue()
    {
        var header = GetHeader();
        var token = LockToken.Next();
        var lockAcquired = false;

        try
        {
            lockAcquired = TryAcquireLock(token, ref header->ReadLockToken, ref header->ReadLockAcquiredTimestampTicks);
            if (!lockAcquired)
                return Error(DequeueErrorType.UnableToAcquireReadLock);

            var readOffset = header->ReadOffset;
            var writeOffset = header->WriteOffset;
            if (readOffset == writeOffset)
                return Error(DequeueErrorType.NothingToRead);

            var sizeBuffer = GetArray(size: 4);
            _buffer.Read(readOffset, 4, sizeBuffer);
            var dataSize = MemoryMarshal.Read<int>(sizeBuffer);
            ReturnArray(sizeBuffer);

            if (dataSize < 0 || dataSize > _options.Capacity)
                ThrowDataCorruptionDetected();

            var resultBuffer = GetArray(size: dataSize);
            _buffer.Read(readOffset + 4, dataSize, resultBuffer);
            ILocalMemory<byte> result = _arrayPool != null
                ? new BorrowedMemory<byte>(resultBuffer, dataSize, _arrayPool)
                : new OwnedMemory<byte>(resultBuffer);

            var nextReadOffset = readOffset + 4 + dataSize;
            if (Interlocked.CompareExchange(ref header->ReadOffset, nextReadOffset, readOffset) != readOffset)
                ThrowDataCorruptionDetected();

            return Ok(result);
        }
        finally
        {
            if (lockAcquired)
            {
                if (Interlocked.CompareExchange(ref header->ReadLockToken, 0, token) != token)
                    ThrowDataCorruptionDetected();
            }
        }
    }

    private bool TryAcquireLock(long token, ref long lockTokenField, ref long lockTimestampField)
    {
        var now = _timeProvider.GetUtcNow();
        var prevToken = Interlocked.CompareExchange(ref lockTokenField, token, 0);
        if (prevToken != 0)
        {
            // Could not acquire lock - check if the previous lock can be invalidated
            var prevTimestamp = Volatile.Read(ref lockTimestampField);
            var prevLockAge = now - new DateTimeOffset(ticks: prevTimestamp, offset: TimeSpan.Zero);
            if (prevLockAge <= _maxLockDuration)
            {
                // Lock is still valid
                return false;
            }
            else
            {
                // Attempt to steal the stale lock: replace the old token with ours
                if (Interlocked.CompareExchange(ref lockTokenField, token, prevToken) != prevToken)
                {
                    // Somebody was faster to acquire the lock
                    return false;
                }
            }
        }

        // Record acquisition timestamp for stale-lock detection by other processes
        Volatile.Write(ref lockTimestampField, now.UtcTicks);
        return true;
    }

    private bool HasEnoughSpace(long dataLength)
    {
        var capacityWithoutHeader = _options.Capacity - Unsafe.SizeOf<QueueHeader>();
        if (dataLength > capacityWithoutHeader)
            return false;

        var header = GetHeader();
        var readOffset = header->ReadOffset;
        var writeOffset = header->WriteOffset;

        var unreadData = writeOffset - readOffset;
        return unreadData + dataLength <= capacityWithoutHeader;
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
