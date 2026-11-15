// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    private void RegisterTaskCompositionBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskCompletionObserved, OnTaskCompletionObserved);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.TaskCompletionForwardToPromise, OnTaskCompletionForwardToPromise);
    }

    private void OnTaskCompletionObserved(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        ObserveCompletedTask(id, new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject));
    }

    private void OnTaskCompletionForwardToPromise(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        var promiseId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject);
        var completedTaskId = new ProcessTrackedObjectId(id.ProcessId, arguments[1].Value.AsTrackedObject);

        // The completion action runs on the thread that saw the task complete
        ObserveCompletedTask(id, completedTaskId);

        // The promise is completed by whichever task finishes last and stands for all of them
        RaiseForkFinished(id, promiseId, ForkKind.Task, ForkFinishKind.PromiseProgressed);
    }

    private void ObserveCompletedTask(ProcessThreadId id, ProcessTrackedObjectId completedTaskId)
    {
        if (completedTaskId.ObjectId.Value != 0)
            ProcessTaskJoinFinish(id, completedTaskId, TaskJoinOutcome.Completed);
    }
}
