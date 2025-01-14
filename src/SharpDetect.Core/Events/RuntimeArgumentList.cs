// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using System.Buffers;
using System.Collections;

namespace SharpDetect.Core.Events;

public record struct RuntimeArgumentList : IReadOnlyList<RuntimeArgumentInfo>, IDisposable
{
    public int Count { get; }
    private readonly RuntimeArgumentInfo[] _arguments;
    private bool _isDisposed;

    public RuntimeArgumentList(RuntimeArgumentInfo[] arguments, int length)
    {
        _arguments = arguments;
        Count = length;
    }

    public readonly RuntimeArgumentInfo this[int index]
    {
        get => index >= 0 && index < Count ? _arguments[index] :
            throw new IndexOutOfRangeException(index.ToString());
    }

    public readonly IEnumerator<RuntimeArgumentInfo> GetEnumerator()
        => _arguments.Take(Count).GetEnumerator();

    readonly IEnumerator IEnumerable.GetEnumerator()
        => _arguments.Take(Count).GetEnumerator();

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        ArrayPool<RuntimeArgumentInfo>.Shared.Return(_arguments);
        GC.SuppressFinalize(this);
    }
}
