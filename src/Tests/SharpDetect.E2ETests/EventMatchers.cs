// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.E2ETests.Utils;
using SharpDetect.TemporalAsserts;

namespace SharpDetect.E2ETests;

public static class EventMatchers
{
    public static AtomicPredicate<ulong, RecordedEventType> EventType(RecordedEventType type) =>
        new(evt => evt.Type == type, description: $"EventType({type})");

    public static AtomicPredicate<ulong, RecordedEventType> MethodEnter(string methodName, TestHappensBeforePlugin plugin)
    {
        return new AtomicPredicate<ulong, RecordedEventType>(
            evt =>
            {
                if (evt.Type != RecordedEventType.MethodEnter)
                    return false;

                var (metadata, args) = evt.Get<(RecordedEventMetadata, MethodEnterRecordedEvent)>();
                var method = plugin.Resolve(metadata, args.ModuleId, args.MethodToken);
                return method.Name.StartsWith(methodName);
            },
            description: $"MethodEnter({methodName})");
    }

    public static AtomicPredicate<ulong, RecordedEventType> MethodExit(string methodName, TestHappensBeforePlugin plugin)
    {
        return new AtomicPredicate<ulong, RecordedEventType>(
            evt =>
            {
                if (evt.Type != RecordedEventType.MethodExit)
                    return false;

                var (metadata, args) = evt.Get<(RecordedEventMetadata, MethodExitRecordedEvent)>();
                var method = plugin.Resolve(metadata, args.ModuleId, args.MethodToken);
                return method.Name.StartsWith(methodName);
            },
            description: $"MethodExit({methodName})");
    }
}