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
        FieldAccessKind? requireAccessKind = null)
    {
        return new AtomicPredicate<ulong, RecordedEventType>(evt =>
        {
            if (evt.Type != type)
                return false;

            RecordedEventMetadata metadata;
            ModuleId moduleId;
            MdMethodDef methodToken;
            FieldAccessKind accessKind;
            switch (type)
            {
                case RecordedEventType.StaticFieldRead:
                    (metadata, var sr) = evt.Get<(RecordedEventMetadata, StaticFieldReadArgs)>();
                    (moduleId, methodToken, accessKind) = (sr.Stack.Top.ModuleId, sr.Stack.Top.MethodToken, sr.AccessKind);
                    break;
                case RecordedEventType.StaticFieldWrite:
                    (metadata, var sw) = evt.Get<(RecordedEventMetadata, StaticFieldWriteArgs)>();
                    (moduleId, methodToken, accessKind) = (sw.Stack.Top.ModuleId, sw.Stack.Top.MethodToken, sw.AccessKind);
                    break;
                case RecordedEventType.InstanceFieldRead:
                    (metadata, var ir) = evt.Get<(RecordedEventMetadata, InstanceFieldReadArgs)>();
                    (moduleId, methodToken, accessKind) = (ir.Stack.Top.ModuleId, ir.Stack.Top.MethodToken, ir.AccessKind);
                    break;
                case RecordedEventType.InstanceFieldWrite:
                    (metadata, var iw) = evt.Get<(RecordedEventMetadata, InstanceFieldWriteArgs)>();
                    (moduleId, methodToken, accessKind) = (iw.Stack.Top.ModuleId, iw.Stack.Top.MethodToken, iw.AccessKind);
                    break;
                default:
                    return false;
            }

            if (requireAccessKind is { } wantAccessKind && accessKind != wantAccessKind)
                return false;

            var resolveResult = plugin.Resolve(metadata, moduleId, methodToken);
            if (resolveResult.IsError)
                return false;

            return resolveResult.Value.Module?.Assembly?.Name?.String == assemblyName;
        }, description: $"FieldAccessInAssembly({type} in {assemblyName}, kind={requireAccessKind?.ToString() ?? "any"})");
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