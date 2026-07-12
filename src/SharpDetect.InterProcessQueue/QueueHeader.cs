// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.InterProcessQueue;

/// <summary>
/// Header placed at byte 0 of every shared-memory queue region.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct QueueHeader
{
    /// <summary>
    /// Identifies SharpDetect IPC queue header. ASCII "SDIPCQUE".
    /// </summary>
    public const long ExpectedMagic = 0x5344_4950_4351_5545;

    [FieldOffset(0)]
    public long Magic;

    [FieldOffset(8)]
    public long Capacity;

    [FieldOffset(16)]
    public long ReadOffset;

    [FieldOffset(24)]
    public long WriteOffset;

    [FieldOffset(32)]
    public long ReadLockToken;

    [FieldOffset(40)]
    public long ReadLockAcquiredTimestampTicks;

    [FieldOffset(48)]
    public long WriteLockToken;

    [FieldOffset(56)]
    public long WriteLockAcquiredTimestampTicks;
}
