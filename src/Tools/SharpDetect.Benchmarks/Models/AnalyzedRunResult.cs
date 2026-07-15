// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Benchmarks.Models;

internal sealed record AnalyzedRunResult(
    double WallSeconds,
    MetricsSnapshot Metrics,
    double AllocatedMB,
    int Gen0,
    int Gen1,
    int Gen2,
    ProcessResourceSample TargetResources,
    double HostPeakRssMB,
    int ReportedIssues);
