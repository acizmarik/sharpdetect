// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    public event Action<ValuePublicationArgs>? ValuePublication;

    private void RegisterValuePublicationBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationStore, OnValueStored);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationLoad, OnValueLoaded);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationStoreLoad, OnValueStoredAndLoaded);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.ValuePublicationLoadByRef, OnValueLoadedByRef);
    }

    private void OnValueStored(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => RaiseFromArgument(metadata, args, ValuePublicationKind.Store);

    private void OnValueLoaded(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => RaiseFromReturnValue(metadata, args, ValuePublicationKind.Load);

    private void OnValueStoredAndLoaded(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => RaiseFromReturnValue(metadata, args, ValuePublicationKind.StoreLoad);

    private void OnValueLoadedByRef(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => RaiseFromArgument(metadata, args, ValuePublicationKind.Load);

    private void RaiseFromArgument(
        RecordedEventMetadata metadata,
        MethodExitWithArgumentsRecordedEvent args,
        ValuePublicationKind kind)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        var value = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject);
        RaiseValuePublication(id, value, kind);
    }

    private void RaiseFromArgument(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args,
        ValuePublicationKind kind)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        var value = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject);
        RaiseValuePublication(id, value, kind);
    }

    private void RaiseFromReturnValue(
        RecordedEventMetadata metadata,
        MethodExitWithArgumentsRecordedEvent args,
        ValuePublicationKind kind)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var raw = MemoryMarshal.Read<nuint>(args.ReturnValue);
        var value = new ProcessTrackedObjectId(id.ProcessId, new TrackedObjectId(raw));
        RaiseValuePublication(id, value, kind);
    }

    private void RaiseValuePublication(ProcessThreadId id, ProcessTrackedObjectId value, ValuePublicationKind kind)
    {
        if (value.ObjectId.Value == 0)
            return;

        ValuePublication?.Invoke(new ValuePublicationArgs(id, value, kind));
    }
}
