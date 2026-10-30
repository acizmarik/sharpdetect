// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    private void RegisterAsyncContinuationBindings()
    {
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.AsyncStateMachineSuspend, OnAsyncStateMachineSuspend);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.AsyncStateMachineResume, OnAsyncStateMachineResume);
        Bind<MethodExitRecordedEvent>(RecordedEventType.AsyncStateMachineSegmentComplete, OnAsyncStateMachineSegmentComplete);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.AsyncStateMachineComplete, OnAsyncStateMachineComplete);
    }
    
    private void OnAsyncStateMachineSuspend(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var raw = MemoryMarshal.Read<nuint>(args.ReturnValue);
        RaiseForkRequested(id, new ProcessTrackedObjectId(id.ProcessId, new TrackedObjectId(raw)), ForkKind.AsyncResumption);
    }

    private void OnAsyncStateMachineResume(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, boxId) = PushSynchronizationContext(metadata, args);
        RaiseForkStarted(id, boxId, ForkKind.AsyncResumption);
    }

    private void OnAsyncStateMachineSegmentComplete(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        ProcessAsyncStateMachineSegmentComplete(id, PopAsyncStateMachineBox(id, args.ModuleId, args.MethodToken));
    }

    private void OnAsyncStateMachineComplete(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        var boxId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject);
        RaiseForkFinished(id, boxId, ForkKind.AsyncResumption, ForkFinishKind.Completed);
    }

    private ProcessTrackedObjectId PopAsyncStateMachineBox(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken)
    {
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, methodToken);
        return new ProcessTrackedObjectId(id.ProcessId, frameLease.Frame.Arguments![0].Value.AsTrackedObject);
    }

    private void ProcessAsyncStateMachineSegmentComplete(ProcessThreadId id, ProcessTrackedObjectId boxId)
        => RaiseForkFinished(id, boxId, ForkKind.AsyncResumption, ForkFinishKind.SegmentCompleted);
}
