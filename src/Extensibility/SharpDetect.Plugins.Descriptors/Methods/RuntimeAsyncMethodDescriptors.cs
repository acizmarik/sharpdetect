// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class RuntimeAsyncMethodDescriptors
{
    private const string RuntimeAsyncTaskTypeName = "System.Runtime.CompilerServices.AsyncHelpers+RuntimeAsyncTask`1";
    private const string RuntimeAsyncAwaitStateTypeName = "System.Runtime.CompilerServices.AsyncHelpers+RuntimeAsyncAwaitState";
    private const string ContinuationTypeName = "System.Runtime.CompilerServices.Continuation";

    private static readonly MethodDescriptor[] AllMethods =
    [
        Suspend("HandleSuspended", Sig.ByRef(Sig.ValueType(RuntimeAsyncAwaitStateTypeName))),
        Suspend("QueueContinuationFollowUpActionIfNecessary", Sig.Class(ContinuationTypeName)),
        Dispatch("DispatchContinuations"),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static MethodDescriptor Suspend(string methodName, ArgumentTypeDescriptor parameterType)
    {
        return RuntimeAsyncTask(methodName, parameterType)
            .Returns(Sig.Boolean)
            .OnEnter(RecordedEventType.RuntimeAsyncSuspend)
            .Build();
    }

    private static MethodDescriptor Dispatch(string methodName)
    {
        return RuntimeAsyncTask(methodName)
            .OnEnter(RecordedEventType.AsyncStateMachineResume)
            .OnExit(RecordedEventType.AsyncStateMachineSegmentComplete)
            .Build();
    }

    private static MethodDescriptorBuilder RuntimeAsyncTask(string methodName, params ArgumentTypeDescriptor[] parameterTypes)
    {
        return MethodDescriptor.ForInstanceMethod(RuntimeAsyncTaskTypeName, methodName)
            .OnRuntime(RuntimeVersions.Net11)
            .Parameters(parameterTypes)
            .CaptureThis();
    }
}
