// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using SharpDetect.E2ETests.Utils;
using SharpDetect.TemporalAsserts;

namespace SharpDetect.E2ETests;

public static class EventMatchers
{
    public static AtomicPredicate<ulong, RecordedEventType> EventType(RecordedEventType type) =>
        new(evt => evt.Type == type, description: $"EventType({type})");

    public static AtomicPredicate<ulong, RecordedEventType> VolatileFieldAccess(RecordedEventType type)
    {
        return new AtomicPredicate<ulong, RecordedEventType>(evt =>
        {
            if (evt.Type != type)
                return false;
        
            return type switch
            {
                RecordedEventType.StaticFieldRead => evt.Get<(RecordedEventMetadata, StaticFieldReadArgs)>().Item2.IsVolatile,
                RecordedEventType.StaticFieldWrite => evt.Get<(RecordedEventMetadata, StaticFieldWriteArgs)>().Item2.IsVolatile,
                RecordedEventType.InstanceFieldRead => evt.Get<(RecordedEventMetadata, InstanceFieldReadArgs)>().Item2.IsVolatile,
                RecordedEventType.InstanceFieldWrite => evt.Get<(RecordedEventMetadata, InstanceFieldWriteArgs)>().Item2.IsVolatile,
                _ => false
            };
        }, description: $"VolatileFieldAccess({type})");
    }

    public static AtomicPredicate<ulong, RecordedEventType> MethodEnter(string methodName, TestExecutionOrderingPlugin plugin)
    {
        return new AtomicPredicate<ulong, RecordedEventType>(evt =>
        {
            if (evt.Type != RecordedEventType.MethodEnter)
                return false;

            var (metadata, args) = evt.Get<(RecordedEventMetadata, MethodEnterRecordedEvent)>();
            var resolveResult = plugin.Resolve(metadata, args.ModuleId, args.MethodToken);
            if (resolveResult.IsError)
                return false;

            var method = resolveResult.Value;
            return method.Name.StartsWith(methodName);
        }, description: $"MethodEnter({methodName})");
    }

    public static AtomicPredicate<ulong, RecordedEventType> MethodExit(string methodName, TestExecutionOrderingPlugin plugin)
    {
        return new AtomicPredicate<ulong, RecordedEventType>(evt =>
        {
            if (evt.Type != RecordedEventType.MethodExit)
                return false;

            var (metadata, args) = evt.Get<(RecordedEventMetadata, MethodExitRecordedEvent)>();
            var resolveResult = plugin.Resolve(metadata, args.ModuleId, args.MethodToken);
            if (resolveResult.IsError)
                return false;

            var method = resolveResult.Value;
            return method.Name.StartsWith(methodName);
        }, description: $"MethodExit({methodName})");
    }
}