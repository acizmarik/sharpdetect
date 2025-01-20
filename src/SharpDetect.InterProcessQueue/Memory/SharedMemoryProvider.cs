// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace SharpDetect.InterProcessQueue.Memory;

internal sealed class SharedMemoryProvider : IDisposable
{
    private readonly MemoryMappedFile _memoryMappedFile;
    private bool _disposed;

    public static SharedMemoryProvider CreateForProducer(string memoryMappingName, string? file, long capacity)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new(MemoryMappedFile.CreateOrOpen(
                memoryMappingName,
                capacity,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.None,
                HandleInheritability.None));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ArgumentNullException.ThrowIfNull(nameof(file));

            return new(MemoryMappedFile.CreateFromFile(
                file!,
                FileMode.OpenOrCreate,
                null,
                capacity));
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }

    public static SharedMemoryProvider CreateForConsumer(string memoryMappingName, string? file, long capacity)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new(MemoryMappedFile.CreateOrOpen(
                memoryMappingName,
                capacity,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.None,
                HandleInheritability.None));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ArgumentNullException.ThrowIfNull(nameof(file));

            return new(MemoryMappedFile.CreateFromFile(
                file!,
                FileMode.OpenOrCreate,
                null,
                capacity,
                MemoryMappedFileAccess.ReadWrite));
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }

    public SharedMemory CreateAccessor()
    {
        var view = _memoryMappedFile.CreateViewAccessor(
            offset: 0,
            size: 0 /* Note: 0 means whole file */,
            access: MemoryMappedFileAccess.ReadWrite);

        return new SharedMemory(view);
    }

    private SharedMemoryProvider(MemoryMappedFile memoryMappedFile)
    {
        _memoryMappedFile = memoryMappedFile;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _memoryMappedFile.Dispose();
    }
}
