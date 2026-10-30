// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    private void RegisterTaskBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskSchedule, OnTaskSchedule);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskStart, OnTaskStart);
        Bind<MethodExitRecordedEvent>(RecordedEventType.TaskComplete, OnTaskComplete);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskJoinStart, OnTaskJoinStart);
        Bind<MethodExitRecordedEvent>(RecordedEventType.TaskJoinFinish, OnTaskJoinFinish);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.TaskJoinFinish, OnTaskJoinFinishWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskPromiseComplete, OnTaskPromiseComplete);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskContinuationRegister, OnTaskContinuationRegister);
    }

    private void OnTaskSchedule(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        // Called on the parent thread when a task is scheduled (e.g. Task.Run)
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject);
        ProcessTaskSchedule(id, taskObjectId);
    }

    private void OnTaskStart(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        // Called on the worker thread when the task body begins executing
        var (id, _, taskObjectId) = PushSynchronizationContext(metadata, args);
        ProcessTaskStart(id, taskObjectId);
    }

    private void OnTaskComplete(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        // Called on the worker thread when the task body finishes executing
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, args.ModuleId, args.MethodToken);
        var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, frameLease.Frame.Arguments![0].Value.AsTrackedObject);
        ProcessTaskComplete(id, taskObjectId);
    }

    private void OnTaskJoinStart(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    private void OnTaskJoinFinish(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        HandleTaskJoinFinish(metadata, args.ModuleId, args.MethodToken, TaskJoinOutcome.Completed);
    }

    private void OnTaskJoinFinishWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var outcome = MemoryMarshal.Read<bool>(args.ReturnValue) ? TaskJoinOutcome.Completed : TaskJoinOutcome.TimedOut;
        HandleTaskJoinFinish(metadata, args.ModuleId, args.MethodToken, outcome);
    }

    private void HandleTaskJoinFinish(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef methodToken,
        TaskJoinOutcome outcome)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, methodToken);
        var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, frameLease.Frame.Arguments![0].Value.AsTrackedObject);

        if (_waitAsyncTaskToSemaphore.Remove(taskObjectId, out var semaphoreId) && outcome == TaskJoinOutcome.Completed)
        {
            // The awaited WaitAsync task completed, so its continuation now holds a permit
            RaisePermitAcquireReturned(new(id, moduleId, methodToken, semaphoreId, IsSuccess: true, IsAsyncContinuation: true));
        }
        else
        {
            ProcessTaskJoinFinish(id, taskObjectId, outcome);
        }
    }

    private void OnTaskPromiseComplete(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        ProcessTaskPromiseComplete(id, new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject));
    }

    private void OnTaskContinuationRegister(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        ProcessTaskContinuationRegister(id, new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject));
    }

    private void ProcessTaskSchedule(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
        => RaiseForkRequested(id, taskObjectId, ForkKind.Task);

    private void ProcessTaskStart(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
        => RaiseForkStarted(id, taskObjectId, ForkKind.Task);

    private void ProcessTaskComplete(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
        => RaiseForkFinished(id, taskObjectId, ForkKind.Task, ForkFinishKind.Completed);

    private void ProcessTaskJoinFinish(ProcessThreadId id, ProcessTrackedObjectId taskObjectId, TaskJoinOutcome outcome)
        => RaiseForkJoined(id, taskObjectId, outcome);

    private void ProcessTaskPromiseComplete(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
        => RaiseForkFinished(id, taskObjectId, ForkKind.Task, ForkFinishKind.PromiseCompleted);

    private void ProcessTaskContinuationRegister(ProcessThreadId id, ProcessTrackedObjectId continuationTaskObjectId)
        => RaiseForkRequested(id, continuationTaskObjectId, ForkKind.Continuation);
}
