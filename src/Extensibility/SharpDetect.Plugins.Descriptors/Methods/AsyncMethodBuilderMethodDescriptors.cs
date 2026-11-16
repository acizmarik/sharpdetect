// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class AsyncMethodBuilderMethodDescriptors
{
    private const string AsyncTaskMethodBuilderTypeName = "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1";
    private const string AsyncStateMachineBoxTypeName = AsyncTaskMethodBuilderTypeName + "+AsyncStateMachineBox`1";
    private const string AsyncStateMachineBoxInterfaceName = "System.Runtime.CompilerServices.IAsyncStateMachineBox";
    private const string TaskTResultTypeName = "System.Threading.Tasks.Task`1";
    private const string ThreadTypeName = "System.Threading.Thread";

    private static ArgumentTypeDescriptor TaskOfTResult => Sig.Generic(TaskTResultTypeName, Sig.TypeParameter(0));

    private static readonly MethodDescriptor[] AllMethods =
    [
        MethodDescriptor.ForStaticMethod(AsyncTaskMethodBuilderTypeName, "GetStateMachineBox")
            .GenericParameters(1)
            .Parameters(Sig.ByRef(Sig.MethodTypeParameter(0)), Sig.ByRef(TaskOfTResult))
            .Returns(Sig.Class(AsyncStateMachineBoxInterfaceName))
            .CaptureReturnValue(Capture.Reference)
            .OnExit(RecordedEventType.AsyncStateMachineSuspend)
            .Build(),
        MethodDescriptor.ForInstanceMethod(AsyncStateMachineBoxTypeName, "MoveNext")
            .Parameters(Sig.Class(ThreadTypeName))
            .CaptureThis()
            .OnEnter(RecordedEventType.AsyncStateMachineResume)
            .OnExit(RecordedEventType.AsyncStateMachineSegmentComplete)
            .Build(),
        MethodDescriptor.ForStaticMethod(AsyncTaskMethodBuilderTypeName, "SetExistingTaskResult")
            .Parameters(TaskOfTResult, Sig.TypeParameter(0))
            .CaptureParameter(0, Capture.Reference)
            .OnEnter(RecordedEventType.AsyncStateMachineComplete)
            .Build(),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;
}
