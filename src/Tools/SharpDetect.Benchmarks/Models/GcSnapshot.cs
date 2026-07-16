// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Benchmarks.Models;

internal readonly record struct GcSnapshot(long AllocatedBytes, int Gen0, int Gen1, int Gen2)
{
    public double AllocatedMegabytes => AllocatedBytes / (1024.0 * 1024.0);

    public static GcSnapshot Capture()
        => new(
            GC.GetTotalAllocatedBytes(precise: true),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));

    public GcSnapshot Since(GcSnapshot before)
        => new(
            AllocatedBytes - before.AllocatedBytes,
            Gen0 - before.Gen0,
            Gen1 - before.Gen1,
            Gen2 - before.Gen2);
}
