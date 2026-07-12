// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.InterProcessQueue;

/// <summary>
/// Header placed at byte 0 of every shared-memory queue region.
/// </summary>
/// <remarks>
/// The queue is single-producer/single-consumer across the process boundary.
/// The producer owns <see cref="WriteOffset"/> and the consumer owns <see cref="ReadOffset"/>.
/// The two are placed on separate 64-byte cache lines so a message does not bounce a shared line between cores.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 192)]
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

    // --- Producer-owned cache line ---

    [FieldOffset(64)]
    public long WriteOffset;

    // --- Consumer-owned cache line ---

    [FieldOffset(128)]
    public long ReadOffset;
}
