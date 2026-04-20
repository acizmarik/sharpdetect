// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.InterProcessQueue;

/// <summary>
/// Header placed at byte 0 of every shared-memory queue region.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 48)]
internal struct QueueHeader
{
    [FieldOffset(0)]
    public long ReadOffset;

    [FieldOffset(8)]
    public long WriteOffset;

    [FieldOffset(16)]
    public long ReadLockToken;

    [FieldOffset(24)]
    public long ReadLockAcquiredTimestampTicks;

    [FieldOffset(32)]
    public long WriteLockToken;

    [FieldOffset(40)]
    public long WriteLockAcquiredTimestampTicks;
}
