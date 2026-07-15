// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using SharpDetect.Benchmarks.Configuration;
using SharpDetect.Benchmarks.Measurements;
using SharpDetect.Benchmarks.Models;

namespace SharpDetect.Benchmarks.Reporting;

internal static class BaselineReportBuilder
{
    private const double SpreadWarningThreshold = 1.2;

    public static BaselineReport Build(
        GitInfo git,
        BenchmarkOptions options,
        IReadOnlyList<BareRunResult> bareRuns,
        IReadOnlyList<AnalyzedRunResult> instrumentedRuns)
    {
        var wallSeconds = ValueSpread.From(instrumentedRuns.Select(static r => r.WallSeconds));
        var processingSeconds = ValueSpread.From(instrumentedRuns.Select(ProcessingSecondsOf));
        var throughputPerSec = ValueSpread.From(instrumentedRuns.Select(static r => r.Metrics.EventsReceived / ProcessingSecondsOf(r)));
        var bareWallSeconds = ValueSpread.From(bareRuns.Select(static r => r.WallSeconds));
        var targetWallSeconds = ValueSpread.From(instrumentedRuns.Select(static r => r.Metrics.TargetWallSeconds));
        var targetCpuSeconds = ValueSpread.From(instrumentedRuns.Select(static r => r.TargetResources.CpuSeconds));
        var bareCpuSeconds = ValueSpread.From(bareRuns.Select(static r => r.Resources.CpuSeconds));

        var warnings = new List<string>();
        AddSpreadWarning(warnings, "throughputPerSec", throughputPerSec);
        AddSpreadWarning(warnings, "targetWallSeconds", targetWallSeconds);
        AddSpreadWarning(warnings, "bareTargetWallSeconds", bareWallSeconds);

        return new BaselineReport(
            Commit: git.Commit,
            CommitDate: git.CommitDate,
            Branch: git.Branch,
            DirtyWorkingTree: git.Dirty,
            CreatedAt: DateTime.UtcNow.ToString("o"),
            Machine: new MachineInfo(
                Os: RuntimeInformation.OSDescription,
                Cores: Environment.ProcessorCount,
                Rid: RuntimeInformation.RuntimeIdentifier,
                CpuAccounting: ChildCpuAccounting.Mode),
            Configuration: BuildInfo.Configuration,
            Workload: new WorkloadInfo("PerfWorkload", options.Threads, options.Iterations),
            Warmup: options.Warmup,
            Runs: options.Runs,
            Warnings: warnings,
            Metrics: new BaselineMetrics(
                WallSeconds: wallSeconds,
                ProcessingSeconds: processingSeconds,
                EventsReceived: ValueSpread.MedianOf(instrumentedRuns.Select(static r => r.Metrics.EventsReceived)),
                EventsProcessed: ValueSpread.MedianOf(instrumentedRuns.Select(static r => r.Metrics.EventsProcessed)),
                ReportedIssues: ValueSpread.MedianOf(instrumentedRuns.Select(static r => (long)r.ReportedIssues)),
                ThroughputPerSec: throughputPerSec,
                DrainTailSeconds: ValueSpread.From(instrumentedRuns.Select(static r => r.Metrics.DrainSeconds)),
                DrainTailEvents: ValueSpread.MedianOf(instrumentedRuns.Select(static r => r.Metrics.DrainEvents)),
                ProcessTailSeconds: ValueSpread.From(instrumentedRuns.Select(static r => r.Metrics.ProcessTailSeconds)),
                HostAllocatedMB: ValueSpread.From(instrumentedRuns.Select(static r => r.AllocatedMB)),
                HostPeakRssMB: ValueSpread.From(instrumentedRuns.Select(static r => r.HostPeakRssMB)),
                HostGcInfo: new GarbageCollectorInfo(
                    ValueSpread.MedianOf(instrumentedRuns.Select(static r => (long)r.Gen0)),
                    ValueSpread.MedianOf(instrumentedRuns.Select(static r => (long)r.Gen1)),
                    ValueSpread.MedianOf(instrumentedRuns.Select(static r => (long)r.Gen2))),
                Target: new TargetMetrics(
                    AnalyzedTargetWallSeconds: targetWallSeconds,
                    AnalyzedTargetCpuSeconds: targetCpuSeconds,
                    AnalyzedTargetPeakRssMB: Math.Round(ValueSpread.MedianOf(instrumentedRuns.Select(static r => r.TargetResources.PeakRssMegabytes)), 1),
                    BareTargetWallSeconds: bareWallSeconds,
                    BareTargetCpuSeconds: bareCpuSeconds,
                    AnalyzedTargetOverheadFactor: Ratio(targetWallSeconds.Median, bareWallSeconds.Median),
                    AnalyzedTargetCpuOverheadFactor: Ratio(targetCpuSeconds.Median, bareCpuSeconds.Median)),
                EventsByType: BuildEventsByType(instrumentedRuns)));
    }

    private static double ProcessingSecondsOf(AnalyzedRunResult run)
        => run.WallSeconds - run.Metrics.ProcessTailSeconds;

    private static double? Ratio(double instrumented, double bare)
        => bare > 0 ? Math.Round(instrumented / bare, 2) : null;

    private static void AddSpreadWarning(List<string> warnings, string metricName, ValueSpread spread)
    {
        if (spread.Min <= 0 || spread.Max / spread.Min <= SpreadWarningThreshold)
            return;

        warnings.Add(
            $"{metricName} is unstable across runs: max/min = {Math.Round(spread.Max / spread.Min, 2)} " +
            $"(> {SpreadWarningThreshold}) — noisy machine or insufficient warmup");
    }

    private static ImmutableDictionary<string, long> BuildEventsByType(IReadOnlyList<AnalyzedRunResult> instrumentedRuns)
    {
        var receivedByTypePerRun = instrumentedRuns.Select(static r => r.Metrics.ReceivedByType).ToArray();
        return receivedByTypePerRun
            .SelectMany(static byType => byType.Keys)
            .Distinct(StringComparer.Ordinal)
            .Select(type => (Type: type, Count: ValueSpread.MedianOf(
                receivedByTypePerRun.Select(byType => byType.GetValueOrDefault(type)))))
            .OrderByDescending(static entry => entry.Count)
            .ToImmutableDictionary(static entry => entry.Type, static entry => entry.Count);
    }
}
