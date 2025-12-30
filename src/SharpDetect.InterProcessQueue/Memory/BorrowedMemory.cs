// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers;

namespace SharpDetect.InterProcessQueue.Memory;

public struct BorrowedMemory<T> : ILocalMemory<T>, IDisposable
{
    private readonly ArrayPool<T> _arrayPool;
    private T[] _array;
    private int _size;
    private bool _disposed;

    internal BorrowedMemory(T[] array, int size, ArrayPool<T> arrayPool)
    {
        _array = array;
        _size = size;
        _arrayPool = arrayPool;
    }

    public readonly ReadOnlySpan<T> Get()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new ReadOnlySpan<T>(_array, 0, _size);
    }

    public readonly ReadOnlyMemory<T> GetLocalMemory()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new ReadOnlyMemory<T>(_array, 0, _size);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _arrayPool.Return(_array);
        _array = [];
        _size = 0;
    }
}
