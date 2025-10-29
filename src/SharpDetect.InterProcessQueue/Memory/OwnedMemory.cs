// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue.Memory;

public readonly struct OwnedMemory<T> : ILocalMemory<T>
{
    private readonly T[] _array;

    public OwnedMemory(T[] array)
    {
        _array = array;
    }

    public ReadOnlySpan<T> Get()
    {
        return _array;
    }

    public ReadOnlyMemory<T> GetLocalMemory()
    {
        return _array;
    }
}
