// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SharpDetect.Core.Events;

public sealed class RuntimeArgumentList : IReadOnlyList<RuntimeArgumentInfo>, IDisposable
{
    private static readonly ConcurrentQueue<RuntimeArgumentList> _pool = new();

    public int Count { get; private set; }
    private RuntimeArgumentInfo[] _arguments;
    private bool _isDisposed;

    internal bool IsDisposed => _isDisposed;

    private RuntimeArgumentList(RuntimeArgumentInfo[] arguments, int length)
    {
        _arguments = arguments;
        Count = length;
    }

    public static RuntimeArgumentList Rent(RuntimeArgumentInfo[] arguments, int length)
    {
        if (_pool.TryDequeue(out var list))
        {
            list._arguments = arguments;
            list.Count = length;
            list._isDisposed = false;
            return list;
        }

        return new RuntimeArgumentList(arguments, length);
    }

    public RuntimeArgumentInfo this[int index]
    {
        get => index >= 0 && index < Count ? _arguments[index] :
            throw new IndexOutOfRangeException(index.ToString());
    }

    public IEnumerator<RuntimeArgumentInfo> GetEnumerator()
        => _arguments.Take(Count).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _arguments.Take(Count).GetEnumerator();

    public void Dispose()
    {
        Debug.Assert(!_isDisposed, "RuntimeArgumentList was disposed twice.");
        if (_isDisposed)
            return;

        _isDisposed = true;
        ArrayPool<RuntimeArgumentInfo>.Shared.Return(_arguments);
        _arguments = [];
        Count = 0;
        _pool.Enqueue(this);
    }
}
