// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue;

internal static unsafe class SharedMemoryHeaderProtocol
{
    public static void InitializeOrValidate(
        long* headerPtr,
        long expectedMagic,
        long configuredCapacity,
        string regionName,
        string kindDescription)
    {
        var magic = Volatile.Read(ref headerPtr[0]);
        if (magic == 0)
        {
            Volatile.Write(ref headerPtr[1], configuredCapacity);
            Volatile.Write(ref headerPtr[0], expectedMagic);
            return;
        }

        if (magic != expectedMagic)
        {
            throw new QueueHeaderValidationException(
                $"Shared memory region \"{regionName}\" is not a recognizable {kindDescription} (magic mismatch).");
        }

        var capacity = Volatile.Read(ref headerPtr[1]);
        if (capacity != configuredCapacity)
        {
            throw new QueueHeaderValidationException(
                $"Shared memory region \"{regionName}\" was created with capacity {capacity} bytes, but this process configured {configuredCapacity} bytes.");
        }
    }
}
