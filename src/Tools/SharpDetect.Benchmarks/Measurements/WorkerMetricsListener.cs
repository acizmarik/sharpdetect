// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;
using SharpDetect.Benchmarks.Models;
using SharpDetect.Worker.Services;

namespace SharpDetect.Benchmarks.Measurements;

internal sealed class WorkerMetricsListener : IDisposable
{
    private readonly MeterListener _listener;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, double> _counters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _gauges = new(StringComparer.Ordinal);

    public WorkerMetricsListener()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = static (instrument, listener) =>
            {
                if (instrument.Meter.Name == AnalysisWorkerMetrics.MeterName)
                    listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _)
            => Record(instrument, measurement, tags));
        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _)
            => Record(instrument, measurement, tags));
        _listener.Start();
    }

    public MetricsSnapshot Poll()
    {
        lock (_lock)
        {
            _listener.RecordObservableInstruments();
            return new MetricsSnapshot(
                Counters: new Dictionary<string, double>(_counters, StringComparer.Ordinal),
                Gauges: new Dictionary<string, double>(_gauges, StringComparer.Ordinal));
        }
    }

    private void Record(Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var store = instrument is ObservableGauge<long> or ObservableGauge<double>
            ? _gauges
            : _counters;

        store[BuildKey(instrument, tags)] = measurement;
    }

    private static string BuildKey(Instrument instrument, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        => tags.IsEmpty ? instrument.Name : $"{instrument.Name}/{tags[0].Value}";

    public void Dispose()
    {
        _listener.Dispose();
    }
}
