// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    private void RegisterSignalBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SignalSet, OnSignalSetEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.SignalSetResult, OnSignalExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.SignalSetResult, OnSignalExitWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SignalReset, OnSignalResetEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.SignalResetResult, OnSignalExit);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SignalWait, OnSignalWaitEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.SignalWaitResult, OnSignalWaitExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.SignalWaitResult, OnSignalWaitExitWithArguments);
    }

    private void OnSignalSetEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, signalId) = PushSynchronizationContext(metadata, args);
        RaiseSignaled(new(id, args.ModuleId, args.MethodToken, signalId, IsAutoReset: false));
    }

    private void OnSignalResetEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, signalId) = PushSynchronizationContext(metadata, args);
        RaiseSignalReset(new(id, args.ModuleId, args.MethodToken, signalId));
    }

    private void OnSignalWaitEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushSynchronizationContext(metadata, args);

    private void OnSignalExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnSignalExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnSignalWaitExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleSignalWaitExit(metadata, args.ModuleId, args.MethodToken, isSuccess: true);

    private void OnSignalWaitExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => HandleSignalWaitExit(metadata, args.ModuleId, args.MethodToken, DetermineSuccessFromExitEvent(metadata, args));

    private void HandleSignalWaitExit(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef methodToken, bool isSuccess)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, methodToken);
        var signalId = ExtractSynchronizationObjectIdFromFrame(id, frameLease.Frame);
        RaiseSignalWaitReturned(new(id, moduleId, methodToken, signalId, IsAutoReset: false, isSuccess));
    }
}
