// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.E2ETests.Utils;
using SharpDetect.TemporalAsserts;

namespace SharpDetect.E2ETests;

public static class EventMatchers
{
    public static AtomicPredicate<ulong, RecordedEventType> EventType(RecordedEventType type) =>
        new(evt => evt.Type == type, description: $"EventType({type})");

    public static AtomicPredicate<ulong, RecordedEventType> FieldAccessInAssembly(
        string assemblyName,
        RecordedEventType type,
        IMetadataResolver plugin,
        bool? requireVolatile = null)
    {
        return new AtomicPredicate<ulong, RecordedEventType>(evt =>
        {
            if (evt.Type != type)
                return false;

            RecordedEventMetadata metadata;
            ModuleId moduleId;
            MdMethodDef methodToken;
            bool isVolatile;
            switch (type)
            {
                case RecordedEventType.StaticFieldRead:
                    (metadata, var sr) = evt.Get<(RecordedEventMetadata, StaticFieldReadArgs)>();
                    (moduleId, methodToken, isVolatile) = (sr.ModuleId, sr.MethodToken, sr.IsVolatile);
                    break;
                case RecordedEventType.StaticFieldWrite:
                    (metadata, var sw) = evt.Get<(RecordedEventMetadata, StaticFieldWriteArgs)>();
                    (moduleId, methodToken, isVolatile) = (sw.ModuleId, sw.MethodToken, sw.IsVolatile);
                    break;
                case RecordedEventType.InstanceFieldRead:
                    (metadata, var ir) = evt.Get<(RecordedEventMetadata, InstanceFieldReadArgs)>();
                    (moduleId, methodToken, isVolatile) = (ir.ModuleId, ir.MethodToken, ir.IsVolatile);
                    break;
                case RecordedEventType.InstanceFieldWrite:
                    (metadata, var iw) = evt.Get<(RecordedEventMetadata, InstanceFieldWriteArgs)>();
                    (moduleId, methodToken, isVolatile) = (iw.ModuleId, iw.MethodToken, iw.IsVolatile);
                    break;
                default:
                    return false;
            }

            if (requireVolatile is { } wantVolatile && isVolatile != wantVolatile)
                return false;

            var resolveResult = plugin.Resolve(metadata, moduleId, methodToken);
            if (resolveResult.IsError)
                return false;

            return resolveResult.Value.Module?.Assembly?.Name?.String == assemblyName;
        }, description: $"FieldAccessInAssembly({type} in {assemblyName}, volatile={requireVolatile?.ToString() ?? "any"})");
    }

    public static AtomicPredicate<ulong, RecordedEventType> MethodEnter(string methodName, IMetadataResolver plugin)
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

    public static AtomicPredicate<ulong, RecordedEventType> MethodExit(string methodName, IMetadataResolver plugin)
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