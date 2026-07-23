// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    public event Action<TaskScheduleArgs>? TaskScheduled;
    public event Action<TaskStartArgs>? TaskStarted;
    public event Action<TaskCompleteArgs>? TaskCompleted;
    public event Action<TaskJoinFinishArgs>? TaskJoinFinished;

    private void RegisterTaskBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskSchedule, OnTaskSchedule);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskStart, OnTaskStart);
        Bind<MethodExitRecordedEvent>(RecordedEventType.TaskComplete, OnTaskComplete);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskJoinStart, OnTaskJoinStart);
        Bind<MethodExitRecordedEvent>(RecordedEventType.TaskJoinFinish, OnTaskJoinFinish);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.TaskJoinFinish, OnTaskJoinFinishWithArguments);
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
        HandleTaskJoinFinish(metadata, args.ModuleId, args.MethodToken, isSuccess: true);
    }

    private void OnTaskJoinFinishWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var isSuccess = MemoryMarshal.Read<bool>(args.ReturnValue);
        HandleTaskJoinFinish(metadata, args.ModuleId, args.MethodToken, isSuccess);
    }

    private void HandleTaskJoinFinish(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef methodToken,
        bool isSuccess)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, methodToken);
        var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, frameLease.Frame.Arguments![0].Value.AsTrackedObject);

        if (_waitAsyncTaskToSemaphore.Remove(taskObjectId, out var semaphoreId) && isSuccess)
        {
            SemaphoreWaitAsyncReturned?.Invoke(new(id, moduleId, methodToken, semaphoreId, taskObjectId));
        }
        else
        {
            ProcessTaskJoinFinish(id, taskObjectId, isSuccess);
        }
    }

    protected virtual void ProcessTaskSchedule(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
    {
        TaskScheduled?.Invoke(new TaskScheduleArgs(id, taskObjectId));
    }

    protected virtual void ProcessTaskStart(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
    {
        TaskStarted?.Invoke(new TaskStartArgs(id, taskObjectId));
    }

    protected virtual void ProcessTaskComplete(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
    {
        TaskCompleted?.Invoke(new TaskCompleteArgs(id, taskObjectId));
    }

    protected virtual void ProcessTaskJoinFinish(ProcessThreadId id, ProcessTrackedObjectId taskObjectId, bool isSuccess)
    {
        TaskJoinFinished?.Invoke(new TaskJoinFinishArgs(id, taskObjectId, isSuccess));
    }
}
