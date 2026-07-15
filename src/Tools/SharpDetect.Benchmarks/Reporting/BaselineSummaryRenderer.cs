// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx.Infrastructure;

namespace SharpDetect.Benchmarks.Reporting;

internal static class BaselineSummaryRenderer
{
    public static void Render(IConsole console, BaselineReport report)
    {
        var metrics = report.Metrics;
        var output = console.Output;
        output.WriteLine();
        output.WriteLine("=== Baseline summary ===");
        output.WriteLine($"{"wall (s, incl. shutdown)",-26} median={metrics.WallSeconds.Median} min={metrics.WallSeconds.Min} max={metrics.WallSeconds.Max}");
        output.WriteLine($"{"processing (s)",-26} median={metrics.ProcessingSeconds.Median} min={metrics.ProcessingSeconds.Min} max={metrics.ProcessingSeconds.Max}");
        output.WriteLine($"{"events received",-26} {metrics.EventsReceived}");
        output.WriteLine($"{"events processed",-26} {metrics.EventsProcessed}");
        output.WriteLine($"{"reported issues",-26} {metrics.ReportedIssues}");
        output.WriteLine($"{"throughput (events/s)",-26} median={metrics.ThroughputPerSec.Median:F0}");
        output.WriteLine($"{"drain tail (s)",-26} median={metrics.DrainTailSeconds.Median} max={metrics.DrainTailSeconds.Max}");
        output.WriteLine($"{"drain tail (events)",-26} {metrics.DrainTailEvents}");
        output.WriteLine($"{"process tail (s)",-26} median={metrics.ProcessTailSeconds.Median} max={metrics.ProcessTailSeconds.Max}");
        output.WriteLine($"{"host alloc (MB)",-26} median={metrics.HostAllocatedMB.Median:F1} (worker + harness)");
        output.WriteLine($"{"host peak RSS (MB)",-26} median={metrics.HostPeakRssMB.Median:F1} (worker + harness)");
        output.WriteLine($"{"host GC gen0/1/2",-26} {metrics.HostGcInfo.Gen0}/{metrics.HostGcInfo.Gen1}/{metrics.HostGcInfo.Gen2}");
        output.WriteLine($"{"target wall (s)",-26} median={metrics.Target.AnalyzedTargetWallSeconds.Median} (bare {metrics.Target.BareTargetWallSeconds.Median})");
        output.WriteLine($"{"target cpu (s)",-26} median={metrics.Target.AnalyzedTargetCpuSeconds.Median} (bare {metrics.Target.BareTargetCpuSeconds.Median}, {report.Machine.CpuAccounting})");
        output.WriteLine($"{"target peak RSS (MB)",-26} {metrics.Target.AnalyzedTargetPeakRssMB}");
        output.WriteLine($"{"overhead factor (wall)",-26} {FormatFactor(metrics.Target.AnalyzedTargetOverheadFactor)}");
        output.WriteLine($"{"overhead factor (cpu)",-26} {FormatFactor(metrics.Target.AnalyzedTargetCpuOverheadFactor)}");
        output.WriteLine("top event types:");
        foreach (var (type, count) in metrics.EventsByType.OrderByDescending(e => e.Value).Take(10))
            output.WriteLine($"  {type,-48} {count}");

        foreach (var warning in report.Warnings)
            output.WriteLine($"WARNING: {warning}");
    }

    private static string FormatFactor(double? factor)
        => factor is { } value ? $"{value}x" : "n/a";
}
