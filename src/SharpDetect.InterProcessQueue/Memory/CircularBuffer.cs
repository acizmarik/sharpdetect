// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using System.Runtime.CompilerServices;

namespace SharpDetect.InterProcessQueue.Memory;

internal unsafe sealed class CircularBuffer
{
    private readonly byte* _pointer;
    private readonly long _capacity;

    public CircularBuffer(byte* pointer, long capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _pointer = pointer;
        _capacity = capacity;
    }

    public void Read(long offset, long length, Memory<byte> result)
    {
        if (length == 0)
            return;

        offset %= _capacity;
        fixed (byte* ptr = result.Span)
            Read(ptr, length, offset);
    }

    public void Write(ReadOnlySpan<byte> data, long offset)
    {
        if (data.Length == 0)
            return;

        offset %= _capacity;
        fixed (byte* ptr = data)
            Write(ptr, data.Length, offset);
    }

    private void Read(byte* destination, long length, long offset)
    {
        EnsureEnoughSpace(length);
        var (firstBlockSize, secondBlockSize) = CalculateBlockSizes(offset, length);
        Buffer.MemoryCopy(_pointer + offset, destination, firstBlockSize, firstBlockSize);
        if (secondBlockSize != 0)
            Buffer.MemoryCopy(_pointer, destination + firstBlockSize, secondBlockSize, secondBlockSize);
    }

    private void Write(byte* source, long length, long offset)
    {
        EnsureEnoughSpace(length);
        var (firstBlockSize, secondBlockSize) = CalculateBlockSizes(offset, length);
        Buffer.MemoryCopy(source, _pointer + offset, firstBlockSize, firstBlockSize);
        if (secondBlockSize != 0)
            Buffer.MemoryCopy(source + firstBlockSize, _pointer, secondBlockSize, secondBlockSize);
    }

    public void Clear()
    {
        long elementsToProcess = _capacity;
        do
        {
            uint elementsToProcessStep = (uint)Math.Min(uint.MaxValue, elementsToProcess);
            Unsafe.InitBlock(_pointer, 0, elementsToProcessStep);
            elementsToProcess -= elementsToProcessStep;
        } while (elementsToProcess > 0);
    }

    private readonly record struct BlockSizes(long FirstBlockSize, long SecondBlockSize);

    private BlockSizes CalculateBlockSizes(long offset, long length)
    {
        var firstBlockSize = Math.Clamp(_capacity - offset, 1, length);
        var secondBlockSize = Math.Clamp(length - firstBlockSize, 0, length - firstBlockSize);
        return new BlockSizes(firstBlockSize, secondBlockSize);
    }

    private void EnsureEnoughSpace(long length)
    {
        if (length > _capacity)
            throw new InvalidOperationException($"Can not work with larger data ({length}) than the size of buffer ({_capacity}).");
    }
}
