// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    public event Action<AsyncStateMachineSuspendArgs>? AsyncStateMachineSuspended;
    public event Action<AsyncStateMachineResumeArgs>? AsyncStateMachineResumed;
    public event Action<AsyncStateMachineSegmentCompleteArgs>? AsyncStateMachineSegmentCompleted;
    public event Action<AsyncStateMachineCompleteArgs>? AsyncStateMachineCompleted;

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
        RaiseAsyncStateMachineSuspend(id, new ProcessTrackedObjectId(id.ProcessId, new TrackedObjectId(raw)));
    }

    private void OnAsyncStateMachineResume(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, boxId) = PushSynchronizationContext(metadata, args);
        RaiseAsyncStateMachineResume(id, boxId);
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
        ProcessAsyncStateMachineComplete(id, new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject));
    }

    private ProcessTrackedObjectId PopAsyncStateMachineBox(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken)
    {
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, methodToken);
        return new ProcessTrackedObjectId(id.ProcessId, frameLease.Frame.Arguments![0].Value.AsTrackedObject);
    }

    private void RaiseAsyncStateMachineSuspend(ProcessThreadId id, ProcessTrackedObjectId boxId)
    {
        if (boxId.ObjectId.Value == 0)
            return;

        ProcessAsyncStateMachineSuspend(id, boxId);
    }

    private void RaiseAsyncStateMachineResume(ProcessThreadId id, ProcessTrackedObjectId boxId)
    {
        if (boxId.ObjectId.Value == 0)
            return;

        ProcessAsyncStateMachineResume(id, boxId);
    }

    protected virtual void ProcessAsyncStateMachineSuspend(ProcessThreadId id, ProcessTrackedObjectId boxId)
    {
        AsyncStateMachineSuspended?.Invoke(new AsyncStateMachineSuspendArgs(id, boxId));
    }

    protected virtual void ProcessAsyncStateMachineResume(ProcessThreadId id, ProcessTrackedObjectId boxId)
    {
        AsyncStateMachineResumed?.Invoke(new AsyncStateMachineResumeArgs(id, boxId));
    }

    protected virtual void ProcessAsyncStateMachineSegmentComplete(ProcessThreadId id, ProcessTrackedObjectId boxId)
    {
        if (boxId.ObjectId.Value == 0)
            return;

        AsyncStateMachineSegmentCompleted?.Invoke(new AsyncStateMachineSegmentCompleteArgs(id, boxId));
    }

    protected virtual void ProcessAsyncStateMachineComplete(ProcessThreadId id, ProcessTrackedObjectId boxId)
    {
        if (boxId.ObjectId.Value == 0)
            return;

        AsyncStateMachineCompleted?.Invoke(new AsyncStateMachineCompleteArgs(id, boxId));
    }
}
