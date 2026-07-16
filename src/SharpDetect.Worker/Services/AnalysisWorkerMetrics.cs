// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Frozen;
using System.Diagnostics.Metrics;
using System.Reflection;
using MessagePack;
using SharpDetect.Core.Events;

namespace SharpDetect.Worker.Services;

public static class AnalysisWorkerMetrics
{
    public const string MeterName = "SharpDetect.Worker";
    public const string EventsReceivedInstrument = "sharpdetect.worker.events.received";
    public const string EventsReceivedByTypeInstrument = "sharpdetect.worker.events.received.by_type";
    public const string EventsProcessedInstrument = "sharpdetect.worker.events.processed";
    public const string DrainEventsInstrument = "sharpdetect.worker.drain.events";
    public const string DrainDurationInstrument = "sharpdetect.worker.drain.duration";
    public const string ProcessTailDurationInstrument = "sharpdetect.worker.process.tail.duration";
    public const string TargetWallDurationInstrument = "sharpdetect.worker.target.wall.duration";
    public const string TargetPidInstrument = "sharpdetect.worker.target.pid";
    private const string EventTypeTagName = "event.type";

    private static readonly Meter Meter = new(MeterName);
    private static readonly ObservableCounter<long> ReceivedByTypeCounter;
    private static readonly Lazy<FrozenDictionary<Type, MutableCount>> ReceivedEventsByType = new(
        static () => typeof(IRecordedEventArgs)
            .GetCustomAttributes<UnionAttribute>()
            .DistinctBy(static union => union.SubType)
            .ToFrozenDictionary(static union => union.SubType, static _ => new MutableCount()),
        LazyThreadSafetyMode.ExecutionAndPublication);
    
    private sealed class MutableCount
    {
        public long Value;
    }

    private static long _receivedEvents;
    private static long _processedEvents;
    private static long _drainedEvents;
    private static double _drainDurationSeconds;
    private static double _processTailDurationSeconds;
    private static double _targetWallDurationSeconds;
    private static long _targetPid;

    static AnalysisWorkerMetrics()
    {
        Meter.CreateObservableCounter(EventsReceivedInstrument, static () => Volatile.Read(ref _receivedEvents),
            unit: "{event}", description: "Total events received from the profiler.");
        ReceivedByTypeCounter = Meter.CreateObservableCounter(EventsReceivedByTypeInstrument, ObserveReceivedEventsByType,
            unit: "{event}", description: "Events received from the profiler, by recorded-event type.");
        Meter.CreateObservableCounter(EventsProcessedInstrument, static () => Volatile.Read(ref _processedEvents),
            unit: "{event}", description: "Events handed over to the plugin host.");
        Meter.CreateObservableCounter(DrainEventsInstrument, static () => Volatile.Read(ref _drainedEvents),
            unit: "{event}", description: "Events received after the target process exited.");
        Meter.CreateObservableCounter(DrainDurationInstrument, static () => Volatile.Read(ref _drainDurationSeconds),
            unit: "s", description: "Cumulative time between target process exit and the last drained event.");
        Meter.CreateObservableCounter(ProcessTailDurationInstrument, static () => Volatile.Read(ref _processTailDurationSeconds),
            unit: "s", description: "Cumulative time between target process exit and the end of event processing.");
        Meter.CreateObservableCounter(TargetWallDurationInstrument, static () => Volatile.Read(ref _targetWallDurationSeconds),
            unit: "s", description: "Cumulative wall-clock lifetime of the target process, from launch to exit.");
        Meter.CreateObservableGauge(TargetPidInstrument, static () => Volatile.Read(ref _targetPid),
            description: "PID of the currently running target process (0 when none).");
    }

    public static long CurrentTargetPid => Volatile.Read(ref _targetPid);

    internal static void EventReceived(IRecordedEventArgs eventArgs)
    {
        _receivedEvents++;
        if (!ReceivedByTypeCounter.Enabled)
            return;

        if (ReceivedEventsByType.Value.TryGetValue(eventArgs.GetType(), out var count))
            count.Value++;
    }

    internal static void EventProcessed()
        => _processedEvents++;

    internal static void EventDrained()
        => _drainedEvents++;

    internal static void DrainCompleted(TimeSpan duration)
        => _drainDurationSeconds += duration.TotalSeconds;

    internal static void ProcessTailCompleted(TimeSpan duration)
        => _processTailDurationSeconds += duration.TotalSeconds;

    internal static void TargetWallCompleted(TimeSpan duration)
        => _targetWallDurationSeconds += duration.TotalSeconds;

    internal static void TargetStarted(uint pid)
        => Volatile.Write(ref _targetPid, pid);

    internal static void TargetExited()
        => Volatile.Write(ref _targetPid, 0);

    private static IEnumerable<Measurement<long>> ObserveReceivedEventsByType()
    {
        foreach (var (type, count) in ReceivedEventsByType.Value)
        {
            var value = Volatile.Read(ref count.Value);
            if (value > 0)
            {
                yield return new Measurement<long>(
                    value,
                    new KeyValuePair<string, object?>(EventTypeTagName, type.Name));
            }
        }
    }
}
