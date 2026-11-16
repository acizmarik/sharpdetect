// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class SignalMethodDescriptors
{
    private const string ManualResetEventSlimTypeName = "System.Threading.ManualResetEventSlim";
    private const string CountdownEventTypeName = "System.Threading.CountdownEvent";
    private const string BarrierTypeName = "System.Threading.Barrier";
    private const string CancellationTokenSourceTypeName = "System.Threading.CancellationTokenSource";
    private const string ParallelReplicaTypeName = "System.Threading.Tasks.TaskReplicator+Replica";
    private const string ParallelReplicaTStateTypeName = "System.Threading.Tasks.TaskReplicator+Replica`1";

    private static readonly MethodDescriptor[] AllMethods =
    [
        Signal(ManualResetEventSlimTypeName, "Set", Sig.Boolean)
            .OnEnter(RecordedEventType.SignalSet)
            .OnExit(RecordedEventType.SignalSetResult)
            .Build(),
        Signal(ManualResetEventSlimTypeName, "Reset")
            .OnEnter(RecordedEventType.SignalReset)
            .OnExit(RecordedEventType.SignalResetResult)
            .Build(),
        Wait(ManualResetEventSlimTypeName, "Wait")
            .OnEnter(RecordedEventType.SignalWait)
            .OnExit(RecordedEventType.SignalWaitResult)
            .Build(),

        Signal(CountdownEventTypeName, "Signal")
            .InAssembly(DeclaringAssemblies.Threading)
            .Returns(Sig.Boolean)
            .OnEnter(RecordedEventType.SignalSet)
            .OnExit(RecordedEventType.SignalSetResult)
            .Build(),
        Signal(CountdownEventTypeName, "Signal", Sig.Int32)
            .InAssembly(DeclaringAssemblies.Threading)
            .Returns(Sig.Boolean)
            .OnEnter(RecordedEventType.SignalSet)
            .OnExit(RecordedEventType.SignalSetResult)
            .Build(),
        Signal(CountdownEventTypeName, "Reset", Sig.Int32)
            .InAssembly(DeclaringAssemblies.Threading)
            .OnEnter(RecordedEventType.SignalReset)
            .OnExit(RecordedEventType.SignalResetResult)
            .Build(),
        Wait(CountdownEventTypeName, "Wait")
            .InAssembly(DeclaringAssemblies.Threading)
            .OnEnter(RecordedEventType.SignalWait)
            .OnExit(RecordedEventType.SignalWaitResult)
            .Build(),

        Wait(BarrierTypeName, "SignalAndWait")
            .InAssembly(DeclaringAssemblies.Threading)
            .OnEnter(RecordedEventType.SignalSet)
            .OnExit(RecordedEventType.SignalWaitResult)
            .Build(),

        Signal(CancellationTokenSourceTypeName, "TransitionToCancellationRequested")
            .Returns(Sig.Boolean)
            .OnEnter(RecordedEventType.SignalSet)
            .OnExit(RecordedEventType.SignalSetResult)
            .Build(),
        Signal(CancellationTokenSourceTypeName, "get_IsCancellationRequested")
            .Returns(Sig.Boolean)
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.SignalWait)
            .OnExit(RecordedEventType.SignalWaitResult)
            .Build(),

        // Internal API behind Parallel.For and Parallel.ForEach
        Signal(ParallelReplicaTStateTypeName, "ExecuteAction", Sig.ByRef(Sig.Boolean))
            .InAssembly(DeclaringAssemblies.ThreadingTasksParallel)
            .OnEnter(RecordedEventType.SignalSetOnReturn)
            .OnExit(RecordedEventType.SignalSetOnReturnResult)
            .Build(),
        Signal(ParallelReplicaTypeName, "Wait")
            .InAssembly(DeclaringAssemblies.ThreadingTasksParallel)
            .OnEnter(RecordedEventType.SignalWait)
            .OnExit(RecordedEventType.SignalWaitResult)
            .Build(),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static MethodDescriptorBuilder Signal(
        string declaringTypeName,
        string methodName,
        params ArgumentTypeDescriptor[] parameterTypes)
    {
        return MethodDescriptor.ForInstanceMethod(declaringTypeName, methodName)
            .Parameters(parameterTypes)
            .CaptureThis();
    }

    private static MethodDescriptorBuilder Wait(string declaringTypeName, string methodName)
    {
        return Signal(declaringTypeName, methodName, Sig.Int32, Sig.CancellationToken)
            .Returns(Sig.Boolean)
            .CaptureReturnValue(Capture.Boolean);
    }
}
