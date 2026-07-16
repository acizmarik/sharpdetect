// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers;

namespace SharpDetect.InterProcessQueue.Memory;

public sealed class BorrowedMemory<T> : ILocalMemory<T>, IDisposable
{
    private readonly ArrayPool<T> _arrayPool;
    private T[] _array;
    private int _size;
    private bool _disposed;

    internal BorrowedMemory(ArrayPool<T> arrayPool)
    {
        _arrayPool = arrayPool;
        _array = [];
    }

    internal void Reset(T[] array, int size)
    {
        _array = array;
        _size = size;
        _disposed = false;
    }

    public ReadOnlySpan<T> Get()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new ReadOnlySpan<T>(_array, 0, _size);
    }

    public ReadOnlyMemory<T> GetLocalMemory()
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
