// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
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
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ArgumentNullException.ThrowIfNull(file);

            return new(MemoryMappedFile.CreateFromFile(
                file,
                FileMode.OpenOrCreate,
                null,
                capacity));
        }

        return ThrowPlatformNotSupported<SharedMemoryProvider>();
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
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ArgumentNullException.ThrowIfNull(file);

            return new(MemoryMappedFile.CreateFromFile(
                file,
                FileMode.OpenOrCreate,
                null,
                capacity,
                MemoryMappedFileAccess.ReadWrite));
        }

        return ThrowPlatformNotSupported<SharedMemoryProvider>();
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

    [DoesNotReturn]
    private static TResult ThrowPlatformNotSupported<TResult>()
    {
        throw new PlatformNotSupportedException($"Memory-mapped queue is not supported on platform: {RuntimeInformation.OSDescription}");
    }
}
