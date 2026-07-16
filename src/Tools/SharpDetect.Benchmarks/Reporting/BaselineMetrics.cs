// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Benchmarks.Models;

namespace SharpDetect.Benchmarks.Reporting;

internal sealed record BaselineMetrics(
    ValueSpread WallSeconds,
    ValueSpread ProcessingSeconds,
    long EventsReceived,
    long EventsProcessed,
    long ReportedIssues,
    ValueSpread ThroughputPerSec,
    ValueSpread DrainTailSeconds,
    long DrainTailEvents,
    ValueSpread ProcessTailSeconds,
    ValueSpread HostAllocatedMB,
    ValueSpread HostPeakRssMB,
    GarbageCollectorInfo HostGcInfo,
    TargetMetrics Target,
    IReadOnlyDictionary<string, long> EventsByType);
