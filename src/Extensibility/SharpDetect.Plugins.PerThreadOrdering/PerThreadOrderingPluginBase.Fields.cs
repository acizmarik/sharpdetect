// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    public event Action<StaticFieldReadArgs>? StaticFieldRead;
    public event Action<StaticFieldWriteArgs>? StaticFieldWritten;
    public event Action<InstanceFieldReadArgs>? InstanceFieldRead;
    public event Action<InstanceFieldWriteArgs>? InstanceFieldWritten;

    private void RegisterFieldAccessBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.StaticFieldRead, OnStaticFieldRead);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.StaticFieldWrite, OnStaticFieldWrite);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.GenericStaticFieldRead, OnStaticFieldRead);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.GenericStaticFieldWrite, OnStaticFieldWrite);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.InstanceFieldRead, OnInstanceFieldRead);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.InstanceFieldWrite, OnInstanceFieldWrite);
    }

    private void OnStaticFieldRead(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var (fieldAccess, declaringTypeHandle) = GetInstrumentedStaticFieldAccessFromArguments(metadata, args);
        StaticFieldRead?.Invoke(new StaticFieldReadArgs(
            id,
            fieldAccess.MethodOffset,
            fieldAccess.FieldToken,
            declaringTypeHandle,
            fieldAccess.AccessKind,
            BuildStack(fieldAccess, args.StackFrames)));
    }

    private void OnStaticFieldWrite(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var (fieldAccess, declaringTypeHandle) = GetInstrumentedStaticFieldAccessFromArguments(metadata, args);
        StaticFieldWritten?.Invoke(new StaticFieldWriteArgs(
            id,
            fieldAccess.MethodOffset,
            fieldAccess.FieldToken,
            declaringTypeHandle,
            fieldAccess.AccessKind,
            BuildStack(fieldAccess, args.StackFrames)));
    }

    private void OnInstanceFieldRead(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var (fieldAccess, instance) = GetInstrumentedInstanceFieldAccessFromArguments(metadata, args);
        InstanceFieldRead?.Invoke(new InstanceFieldReadArgs(
            id,
            fieldAccess.MethodOffset,
            fieldAccess.FieldToken,
            instance,
            fieldAccess.AccessKind,
            BuildStack(fieldAccess, args.StackFrames)));
    }

    private void OnInstanceFieldWrite(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var (fieldAccess, instance) = GetInstrumentedInstanceFieldAccessFromArguments(metadata, args);
        InstanceFieldWritten?.Invoke(new InstanceFieldWriteArgs(
            id,
            fieldAccess.MethodOffset,
            fieldAccess.FieldToken,
            instance,
            fieldAccess.AccessKind,
            BuildStack(fieldAccess, args.StackFrames)));
    }

    private static CapturedStackTrace BuildStack(InstrumentedFieldAccess fieldAccess, byte[]? deeperFramesBlob)
        => new(new CapturedStackFrame(fieldAccess.ModuleId, fieldAccess.MethodToken), deeperFramesBlob);

    private (InstrumentedFieldAccess Access, nuint DeclaringTypeHandle) GetInstrumentedStaticFieldAccessFromArguments(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var instrumentationId = MemoryMarshal.Read<ulong>(args.ArgumentValues);
        var access = InstrumentedFieldAccesses[new InstrumentationPointId(metadata.Pid, instrumentationId)];
        var declaringTypeHandle = args.ArgumentValues.Length > sizeof(ulong)
            ? MemoryMarshal.Read<nuint>(args.ArgumentValues.AsSpan()[sizeof(ulong)..])
            : 0;
        return (access, declaringTypeHandle);
    }

    private (InstrumentedFieldAccess Access, ProcessTrackedObjectId Instance) GetInstrumentedInstanceFieldAccessFromArguments(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var instrumentationId = MemoryMarshal.Read<ulong>(args.ArgumentValues);
        var access = InstrumentedFieldAccesses[new InstrumentationPointId(metadata.Pid, instrumentationId)];
        var instanceId = MemoryMarshal.Read<nuint>(args.ArgumentValues.AsSpan()[sizeof(ulong)..]);
        var instance = new ProcessTrackedObjectId(metadata.Pid, new TrackedObjectId(instanceId));
        return (access, instance);
    }
}
