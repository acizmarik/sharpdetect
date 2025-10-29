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

internal unsafe sealed class MemoryMappedQueue : IDisposable
{
    public readonly MemoryMappedQueueOptions Options;
    internal readonly CircularBuffer Buffer;
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
        Options = options;
        var headerSize = Unsafe.SizeOf<QueueHeader>();
        var capacityWithoutHeader = options.Capacity - headerSize;
        var minimumViableSize = headerSize + sizeof(int) + 1;
        if (Options.Capacity < minimumViableSize)
            throw new Exception("Capacity is too small for a queue to function properly.");

        _arrayPool = arrayPool;
        _timeProvider = timeProvider;

        _sharedMemoryProvider = Options switch
        {
            ConsumerMemoryMappedQueueOptions => SharedMemoryProvider.CreateForConsumer(options.Name, options.File, options.Capacity),
            ProducerMemoryMappedQueueOptions => SharedMemoryProvider.CreateForProducer(options.Name, options.File, options.Capacity),
            MemoryMappedQueueOptions => SharedMemoryProvider.CreateForProducer(options.Name, options.File, options.Capacity),
            _ => throw new NotSupportedException(Options.GetType().FullName)
        };
        _sharedMemory = _sharedMemoryProvider.CreateAccessor();

        _maxLockDuration = TimeSpan.FromSeconds(10);
        Buffer = new CircularBuffer(_sharedMemory.GetPointer() + headerSize, capacityWithoutHeader);
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
            Buffer.Write(sizeBuffer, writeOffset);
            Buffer.Write(data, writeOffset + 4);

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
            Buffer.Read(readOffset, 4, sizeBuffer);
            var dataSize = MemoryMarshal.Read<int>(sizeBuffer);
            ReturnArray(sizeBuffer);

            var resultBuffer = GetArray(size: dataSize);
            Buffer.Read(readOffset + 4, dataSize, resultBuffer);
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

    public Status<EnqueueErrorType, DequeueErrorType> Clear()
    {
        var header = GetHeader();
        var timestampUtc = _timeProvider.GetUtcNow();
        var timestampUtcTicks = timestampUtc.UtcTicks;
        bool writeLockAcquired = false;
        bool readLockAcquired = false;

        try
        {
            writeLockAcquired = TryAcquireLock(timestampUtc, ref header->WriteLockTimeStampTicksUtc);
            if (!writeLockAcquired)
                Error(EnqueueErrorType.UnableToAcquireWriteLock);

            readLockAcquired = TryAcquireLock(timestampUtc, ref header->ReadLockTimeStampTicksUtc);
            if (!readLockAcquired)
                Error(DequeueErrorType.UnableToAcquireReadLock);

            header->ReadOffset = 0;
            header->WriteOffset = 0;
            Buffer.Clear();
        }
        finally
        {
            if (writeLockAcquired)
            {
                if (Interlocked.CompareExchange(ref header->WriteLockTimeStampTicksUtc, 0, timestampUtcTicks) != timestampUtcTicks)
                    ThrowDataCorruptionDetected();
            }

            if (readLockAcquired)
            {
                if (Interlocked.CompareExchange(ref header->ReadLockTimeStampTicksUtc, 0, timestampUtcTicks) != timestampUtcTicks)
                    ThrowDataCorruptionDetected();
            }
        }

        return Ok();
    }

    internal bool TryAcquireLock(DateTimeOffset timestampUtc, ref long lockField)
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

    internal bool HasEnoughSpace(long dataLength)
    {
        var capacityWithoutHeader = Options.Capacity - Unsafe.SizeOf<QueueHeader>();
        if (dataLength > capacityWithoutHeader)
            return false;

        var header = GetHeader();
        var readOffset = header->ReadOffset;
        var writeOffset = header->WriteOffset;

        var unreadData = writeOffset - readOffset;
        return unreadData + dataLength <= capacityWithoutHeader;
    }

    internal QueueHeader* GetHeader()
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
