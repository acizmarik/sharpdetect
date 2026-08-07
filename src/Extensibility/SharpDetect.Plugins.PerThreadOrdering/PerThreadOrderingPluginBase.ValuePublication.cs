// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    private const int ContainerArgumentIndex = 0;
    private const int ValueArgumentIndex = 1;

    public event Action<ValuePublicationArgs>? ValuePublication;

    private void RegisterValuePublicationBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationContainerEnter, OnValueContainerEntered);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationStore, OnValueStored);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationLoad, OnValueLoaded);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationStoreLoad, OnValueStoredAndLoaded);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationLoadByRef, OnValueLoadedByRef);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationMaybeStoreLoad, OnValueMaybeStoredAndLoaded);
    }

    private void OnValueContainerEntered(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    private void OnValueStored(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        RaiseValuePublication(
            id,
            GetCapturedObject(id, arguments, ContainerArgumentIndex),
            GetCapturedObject(id, arguments, ValueArgumentIndex),
            ValuePublicationKind.Store);
    }

    private void OnValueLoaded(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => RaiseFromReturnValue(metadata, args, ValuePublicationKind.Load);

    private void OnValueStoredAndLoaded(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => RaiseFromReturnValue(metadata, args, ValuePublicationKind.StoreLoad);

    private void OnValueMaybeStoredAndLoaded(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => RaiseFromReturnValue(metadata, args, ValuePublicationKind.MaybeStoreLoad);

    private void OnValueLoadedByRef(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var container = PopValuePublicationContainer(id, args.ModuleId, args.MethodToken);
        using var arguments = ParseArguments(metadata, args);
        RaiseValuePublication(id, container, GetCapturedObject(id, arguments, 0), ValuePublicationKind.Load);
    }

    private void RaiseFromReturnValue(
        RecordedEventMetadata metadata,
        MethodExitWithArgumentsRecordedEvent args,
        ValuePublicationKind kind)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var container = PopValuePublicationContainer(id, args.ModuleId, args.MethodToken);
        var raw = MemoryMarshal.Read<nuint>(args.ReturnValue);
        var value = new ProcessTrackedObjectId(id.ProcessId, new TrackedObjectId(raw));
        RaiseValuePublication(id, container, value, kind);
    }

    private ProcessTrackedObjectId PopValuePublicationContainer(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken)
    {
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, methodToken);
        return new ProcessTrackedObjectId(
            id.ProcessId,
            frameLease.Frame.Arguments![ContainerArgumentIndex].Value.AsTrackedObject);
    }

    private static ProcessTrackedObjectId GetCapturedObject(ProcessThreadId id, RuntimeArgumentList arguments, int position)
        => new(id.ProcessId, arguments[position].Value.AsTrackedObject);

    private void RaiseValuePublication(
        ProcessThreadId id,
        ProcessTrackedObjectId container,
        ProcessTrackedObjectId value,
        ValuePublicationKind kind)
    {
        if (value.ObjectId.Value == 0 || container.ObjectId.Value == 0)
            return;

        ValuePublication?.Invoke(new ValuePublicationArgs(id, container, value, kind));
    }
}
