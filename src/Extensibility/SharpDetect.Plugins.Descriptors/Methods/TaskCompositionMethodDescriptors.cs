// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class TaskCompositionMethodDescriptors
{
    private const string TaskTypeName = "System.Threading.Tasks.Task";
    private const string WhenAllPromiseTypeName = TaskTypeName + "+WhenAllPromise";
    private const string WhenAllPromiseTResultTypeName = TaskTypeName + "+WhenAllPromise`1";
    private const string CountdownPromiseTypeName = "System.Threading.Tasks.TaskFactory+CompleteOnCountdownPromise";
    private const string CountdownPromiseTResultTypeName = "System.Threading.Tasks.TaskFactory+CompleteOnCountdownPromise`1";

    private static readonly MethodDescriptor[] AllMethods =
    [
        MethodDescriptor.ForInstanceMethod(TaskTypeName, "get_IsWaitNotificationEnabled")
            .Returns(Sig.Boolean)
            .CaptureThis()
            .OnEnter(RecordedEventType.TaskCompletionObserved)
            .Build(),

        // Internal API: invoked once per awaited task, on the thread that observed its completion
        CompletionAction(WhenAllPromiseTypeName),
        CompletionAction(WhenAllPromiseTResultTypeName),

        // Internal API behind TaskFactory.ContinueWhenAll, which counts completions the same way
        CompletionAction(CountdownPromiseTypeName),
        CompletionAction(CountdownPromiseTResultTypeName),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static MethodDescriptor CompletionAction(string declaringTypeName)
    {
        return MethodDescriptor.ForInstanceMethod(declaringTypeName, "Invoke")
            .Parameters(Sig.Class(TaskTypeName))
            .CaptureThis()
            .CaptureParameter(0, Capture.Reference)
            .OnEnter(RecordedEventType.TaskCompletionForwardToPromise)
            .Build();
    }
}
