// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.Memory;

namespace SharpDetect.InterProcessQueue;

public sealed unsafe class RegistrationTable : IDisposable
{
    /// <summary>ASCII "SDIPCREG" — identifies a registration-table region.</summary>
    private const long ExpectedMagic = 0x5344_4950_4352_4547;
    // Header: long[0] = magic, long[1] = capacity. Slots begin at byte 64 (long index 8).
    private const int SlotsStartLongIndex = 8;

    private readonly long _capacity;
    private readonly int _maxSlots;
    private readonly SharedMemoryProvider _sharedMemoryProvider;
    private readonly SharedMemory _sharedMemory;
    private int _scanCursor;
    private bool _disposed;

    public RegistrationTable(string name, string? file, long capacity, bool createAsConsumer)
    {
        var minimumViableSize = (SlotsStartLongIndex + 1) * sizeof(long);
        if (capacity < minimumViableSize)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"Capacity must be at least {minimumViableSize} bytes.");

        _capacity = capacity;
        _maxSlots = (int)((capacity - SlotsStartLongIndex * sizeof(long)) / sizeof(long));
        _sharedMemoryProvider = createAsConsumer
            ? SharedMemoryProvider.CreateForConsumer(name, file, capacity)
            : SharedMemoryProvider.CreateForProducer(name, file, capacity);
        _sharedMemory = _sharedMemoryProvider.CreateAccessor();
        InitializeOrValidateHeader(name);
    }

    private void InitializeOrValidateHeader(string name)
    {
        var header = (long*)_sharedMemory.GetPointer();
        SharedMemoryHeaderProtocol.InitializeOrValidate(header, ExpectedMagic, _capacity, name, "SharpDetect registration table");
    }

    public void Register(uint pid)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (pid == 0)
            throw new ArgumentOutOfRangeException(nameof(pid), "A process id of 0 cannot be registered.");

        var slots = Slots();
        var value = (long)pid;
        for (var i = 0; i < _maxSlots; i++)
        {
            var existing = Volatile.Read(ref slots[i]);
            if (existing == value)
                return;
            if (existing == 0 && Interlocked.CompareExchange(ref slots[i], value, 0) == 0)
                return;
        }

        throw new InvalidOperationException($"Registration table is full ({_maxSlots} slots).");
    }
    
    public IReadOnlyList<uint> DrainNewRegistrations()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var slots = Slots();
        List<uint>? newPids = null;
        while (_scanCursor < _maxSlots)
        {
            var pid = Volatile.Read(ref slots[_scanCursor]);
            if (pid == 0)
                break;

            (newPids ??= new List<uint>()).Add((uint)pid);
            _scanCursor++;
        }

        return (IReadOnlyList<uint>?)newPids ?? Array.Empty<uint>();
    }

    private long* Slots()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return (long*)_sharedMemory.GetPointer() + SlotsStartLongIndex;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _sharedMemory.Dispose();
        _sharedMemoryProvider.Dispose();
    }
}
