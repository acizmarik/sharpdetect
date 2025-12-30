// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.IO.MemoryMappedFiles;

namespace SharpDetect.InterProcessQueue.Memory;

internal sealed unsafe class SharedMemory : IMemory<byte>, IDisposable
{
    private readonly MemoryMappedViewAccessor _view;
    private readonly byte* _acquiredPointer;
    private bool _disposed;

    public SharedMemory(MemoryMappedViewAccessor view)
    {
        _view = view;
        _view.SafeMemoryMappedViewHandle.AcquirePointer(ref _acquiredPointer);
        if (_acquiredPointer == null)
            throw new InvalidOperationException("Failed to acquire a valid pointer to the memory-mapped view. The view handle may be invalid or closed.");
    }

    public byte* GetPointer()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _acquiredPointer;
    }

    public ReadOnlySpan<byte> Get()
    {
        return new ReadOnlySpan<byte>(GetPointer(), (int)_view.Capacity);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_acquiredPointer != null)
            _view.SafeMemoryMappedViewHandle.ReleasePointer();
        _view.Flush();
        _view.Dispose();
    }
}
