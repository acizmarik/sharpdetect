// Copyright 2025 Andrej Čižmárik and Contributors
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

    private MemoryMappedQueue(MemoryMappedQueueOptions options, ArrayPool<byte>? arrayPool, TimeProvider timeProvider)
    {
        _options = options;
        var headerSize = Unsafe.SizeOf<QueueHeader>();
        var capacityWithoutHeader = options.Capacity - headerSize;
        var minimumViableSize = headerSize + sizeof(int) + 1;
        if (_options.Capacity < minimumViableSize)
            throw new Exception("Capacity is too small for a queue to function properly.");

        _arrayPool = arrayPool;
        _timeProvider = timeProvider;

        _sharedMemoryProvider = _options switch
        {
            ConsumerMemoryMappedQueueOptions => SharedMemoryProvider.CreateForConsumer(options.Name, options.File, options.Capacity),
            ProducerMemoryMappedQueueOptions => SharedMemoryProvider.CreateForProducer(options.Name, options.File, options.Capacity),
            _ => throw new NotSupportedException(_options.GetType().FullName)
        };
        _sharedMemory = _sharedMemoryProvider.CreateAccessor();

        _maxLockDuration = TimeSpan.FromSeconds(10);
        _buffer = new CircularBuffer(_sharedMemory.GetPointer() + headerSize, capacityWithoutHeader);
    }

    ~MemoryMappedQueue()
    {
        Dispose();
    }

    public Status<EnqueueErrorType> Enqueue(ReadOnlySpan<byte> data)
    {
        var header = GetHeader();
        var timestampUtc = _timeProvider.GetUtcNow();
        var timestampUtcTicks = timestampUtc.UtcTicks;
        var lockAcquired = false;

        try
        {
            lockAcquired = TryAcquireLock(timestampUtc, ref header->WriteLockTimeStampTicksUtc);
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
                if (Interlocked.CompareExchange(ref header->WriteLockTimeStampTicksUtc, 0, timestampUtcTicks) != timestampUtcTicks)
                    ThrowDataCorruptionDetected();
            }
        }

        return Ok();
    }

    public Result<ILocalMemory<byte>, DequeueErrorType> Dequeue()
    {
        var header = GetHeader();
        var timestampUtc = _timeProvider.GetUtcNow();
        var timestampUtcTicks = timestampUtc.UtcTicks;
        var lockAcquired = false;

        try
        {
            lockAcquired = TryAcquireLock(timestampUtc, ref header->ReadLockTimeStampTicksUtc);
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
                if (Interlocked.CompareExchange(ref header->ReadLockTimeStampTicksUtc, 0, timestampUtcTicks) != timestampUtcTicks)
                    ThrowDataCorruptionDetected();
            }
        }
    }

    private bool TryAcquireLock(DateTimeOffset timestampUtc, ref long lockField)
    {
        var timestampUtcTicks = timestampUtc.UtcTicks;
        var lastLockTimeStamp = Interlocked.CompareExchange(ref lockField, timestampUtcTicks, 0);
        if (lastLockTimeStamp != 0)
        {
            // Could not acquire lock - check if the previous lock can be invalidated
            var lastLockDuration = timestampUtc - new DateTimeOffset(ticks: lastLockTimeStamp, offset: TimeSpan.Zero);
            if (lastLockDuration <= _maxLockDuration)
            {
                // Lock is still valid
                return false;
            }
            else
            {
                // Attempt to clear previous lock and acquire for ourselves
                var result = Interlocked.CompareExchange(ref lockField, timestampUtcTicks, lastLockTimeStamp);
                if (result != lastLockTimeStamp)
                {
                    // Somebody was faster to acquire the lock
                    return false;
                }
            }
        }

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
        throw new Exception("Detected shared buffer corruption.");
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
