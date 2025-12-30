// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.InterProcessQueue;

[StructLayout(LayoutKind.Explicit, Size = 32)]
internal struct QueueHeader
{
    [FieldOffset(0)]
    public long ReadOffset;

    [FieldOffset(8)]
    public long WriteOffset;

    [FieldOffset(16)]
    public long ReadLockTimeStampTicksUtc;

    [FieldOffset(24)]
    public long WriteLockTimeStampTicksUtc;
}
