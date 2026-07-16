// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Services;

namespace SharpDetect.Benchmarks.Models;

internal sealed record MetricsSnapshot(
    IReadOnlyDictionary<string, double> Counters,
    IReadOnlyDictionary<string, double> Gauges)
{
    private const string ReceivedByTypePrefix = AnalysisWorkerMetrics.EventsReceivedByTypeInstrument + "/";
    public long EventsReceived => (long)Counters.GetValueOrDefault(AnalysisWorkerMetrics.EventsReceivedInstrument);
    public long EventsProcessed => (long)Counters.GetValueOrDefault(AnalysisWorkerMetrics.EventsProcessedInstrument);
    public long DrainEvents => (long)Counters.GetValueOrDefault(AnalysisWorkerMetrics.DrainEventsInstrument);
    public double DrainSeconds => Counters.GetValueOrDefault(AnalysisWorkerMetrics.DrainDurationInstrument);
    public double ProcessTailSeconds => Counters.GetValueOrDefault(AnalysisWorkerMetrics.ProcessTailDurationInstrument);
    public double TargetWallSeconds => Counters.GetValueOrDefault(AnalysisWorkerMetrics.TargetWallDurationInstrument);
    public long TargetPid => (long)Gauges.GetValueOrDefault(AnalysisWorkerMetrics.TargetPidInstrument);

    public IReadOnlyDictionary<string, long> ReceivedByType
        => Counters
            .Where(static pair => pair.Key.StartsWith(ReceivedByTypePrefix, StringComparison.Ordinal))
            .ToDictionary(
                static pair => pair.Key[ReceivedByTypePrefix.Length..],
                static pair => (long)pair.Value,
                StringComparer.Ordinal);

    public static MetricsSnapshot Delta(MetricsSnapshot before, MetricsSnapshot after)
    {
        var counters = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (key, value) in after.Counters)
        {
            var delta = value - before.Counters.GetValueOrDefault(key);
            if (delta > 0)
                counters[key] = delta;
        }

        return new MetricsSnapshot(counters, after.Gauges);
    }
}