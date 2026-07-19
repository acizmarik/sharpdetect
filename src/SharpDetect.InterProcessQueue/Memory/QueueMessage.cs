// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue.Memory;

public readonly struct QueueMessage : IDisposable
{
    private readonly MemoryMappedQueue? _owner;
    private readonly byte[] _array;
    private readonly int _size;
    private readonly int _version;

    internal QueueMessage(MemoryMappedQueue owner, byte[] array, int size, int version)
    {
        _owner = owner;
        _array = array;
        _size = size;
        _version = version;
    }

    public ReadOnlySpan<byte> Span => new(ValidatedArray(), 0, _size);

    public ReadOnlyMemory<byte> Memory => new(ValidatedArray(), 0, _size);
    
    public void Dispose()
        => _owner?.Release(_version);

    private byte[] ValidatedArray()
    {
        if (_owner is null)
            throw new InvalidOperationException($"This {nameof(QueueMessage)} was never dequeued from a queue.");

        _owner.ValidateCurrent(_version);
        return _array;
    }
}
