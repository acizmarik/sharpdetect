// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.IO.MemoryMappedFiles;
using Microsoft.Win32.SafeHandles;

namespace SharpDetect.InterProcessQueue.Memory;

internal sealed unsafe class SharedMemory : SafeHandleZeroOrMinusOneIsInvalid, IMemory<byte>
{
    private readonly MemoryMappedViewAccessor _view;

    public SharedMemory(MemoryMappedViewAccessor view)
        : base(ownsHandle: true)
    {
        _view = view;

        byte* pointer = null;
        _view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
        if (pointer == null)
        {
            _view.SafeMemoryMappedViewHandle.ReleasePointer();
            throw new InvalidOperationException("Failed to acquire a valid pointer to the memory-mapped view. The view handle may be invalid or closed.");
        }

        SetHandle((nint)pointer);
    }

    public byte* GetPointer()
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return (byte*)handle;
    }

    public ReadOnlySpan<byte> Get()
    {
        return new ReadOnlySpan<byte>(GetPointer(), (int)_view.Capacity);
    }

    protected override bool ReleaseHandle()
    {
        _view.SafeMemoryMappedViewHandle.ReleasePointer();
        _view.Dispose();
        return true;
    }
}
