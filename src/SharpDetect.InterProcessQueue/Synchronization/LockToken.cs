// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue.Synchronization;

/// <summary>
/// Generates unique 64-bit lock tokens for the shared-memory spin lock.
/// The token is a composite (upper 32 bits - PID; lower 32 bits - sequence number)
/// </summary>
internal static class LockToken
{
    private static int _sequenceNumber;

    public static long Next()
    {
        // Use uint to avoid negative values after int overflow; sign-extend to long.
        var sequence = (uint)Interlocked.Increment(ref _sequenceNumber);
        var pid = (uint)Environment.ProcessId;
        return (long)((ulong)pid << 32 | sequence);
    }
}
