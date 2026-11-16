// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class TaskMethodDescriptors
{
    private const string TaskTypeName = "System.Threading.Tasks.Task";
    private const string TaskTResultTypeName = "System.Threading.Tasks.Task`1";
    private const string ContinuationTaskFromTaskTypeName = "System.Threading.Tasks.ContinuationTaskFromTask";
    private const string ContinuationResultTaskFromTaskTypeName = "System.Threading.Tasks.ContinuationResultTaskFromTask`1";
    private const string ContinuationTaskFromResultTaskTypeName = "System.Threading.Tasks.ContinuationTaskFromResultTask`1";
    private const string ContinuationResultTaskFromResultTaskTypeName = "System.Threading.Tasks.ContinuationResultTaskFromResultTask`2";
    private const string TaskAwaiterTypeName = "System.Runtime.CompilerServices.TaskAwaiter";
    private const string ConfigureAwaitOptionsTypeName = "System.Threading.Tasks.ConfigureAwaitOptions";
    private const string TaskSchedulerTypeName = "System.Threading.Tasks.TaskScheduler";
    private const string TaskContinuationOptionsTypeName = "System.Threading.Tasks.TaskContinuationOptions";

    private static readonly MethodDescriptor[] AllMethods =
    [
        // Internal API
        MethodDescriptor.ForInstanceMethod(TaskTypeName, "ScheduleAndStart")
            .Parameters(Sig.Boolean)
            .CaptureThis()
            .OnEnter(RecordedEventType.TaskSchedule)
            .Build(),
        CreateInnerInvoke(TaskTypeName),
        CreateInnerInvoke(TaskTResultTypeName),
        CreateInnerInvoke(ContinuationTaskFromTaskTypeName),
        CreateInnerInvoke(ContinuationResultTaskFromTaskTypeName),
        CreateInnerInvoke(ContinuationTaskFromResultTaskTypeName),
        CreateInnerInvoke(ContinuationResultTaskFromResultTaskTypeName),
        MethodDescriptor.ForStaticMethod(TaskAwaiterTypeName, "ValidateEnd")
            .Parameters(Sig.Class(TaskTypeName), Sig.ValueType(ConfigureAwaitOptionsTypeName))
            .CaptureParameter(0, Capture.Reference)
            .OnEnter(RecordedEventType.TaskJoinStart)
            .OnExit(RecordedEventType.TaskJoinFinish)
            .Build(),
        CreatePromiseCompletion(TaskTResultTypeName, "TrySetResult", Sig.TypeParameter(0)),
        CreatePromiseCompletion(TaskTypeName, "TrySetResult"),
        CreatePromiseCompletion(TaskTypeName, "TrySetException", Sig.Object),
        CreatePromiseCompletion(TaskTypeName, "TrySetCanceled", Sig.CancellationToken),
        CreatePromiseCompletion(TaskTypeName, "TrySetCanceled", Sig.CancellationToken, Sig.Object),
        MethodDescriptor.ForInstanceMethod(TaskTypeName, "ContinueWithCore")
            .Parameters(
                Sig.Class(TaskTypeName),
                Sig.Class(TaskSchedulerTypeName),
                Sig.CancellationToken,
                Sig.ValueType(TaskContinuationOptionsTypeName))
            .CaptureParameter(0, Capture.Reference)
            .OnEnter(RecordedEventType.TaskContinuationRegister)
            .Build(),

        // Common public API
        MethodDescriptor.ForInstanceMethod(TaskTypeName, "Wait")
            .Parameters(Sig.Int32, Sig.CancellationToken)
            .Returns(Sig.Boolean)
            .CaptureThis()
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.TaskJoinStart)
            .OnExit(RecordedEventType.TaskJoinFinish)
            .Build(),
        MethodDescriptor.ForInstanceMethod(TaskTResultTypeName, "get_Result")
            .Returns(Sig.TypeParameter(0))
            .CaptureThis()
            .OnEnter(RecordedEventType.TaskJoinStart)
            .OnExit(RecordedEventType.TaskJoinFinish)
            .Build(),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static MethodDescriptor CreatePromiseCompletion(
        string typeName,
        string methodName,
        params ArgumentTypeDescriptor[] parameterTypes)
    {
        return MethodDescriptor.ForInstanceMethod(typeName, methodName)
            .Parameters(parameterTypes)
            .Returns(Sig.Boolean)
            .CaptureThis()
            .OnEnter(RecordedEventType.TaskPromiseComplete)
            .Build();
    }

    private static MethodDescriptor CreateInnerInvoke(string typeName)
    {
        return MethodDescriptor.ForInstanceMethod(typeName, "InnerInvoke")
            .CaptureThis()
            .OnEnter(RecordedEventType.TaskStart)
            .OnExit(RecordedEventType.TaskComplete)
            .Build();
    }
}
