// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class LockMethodDescriptors
{
    private const string LockTypeName = "System.Threading.Lock";
    private const string LockScopeTypeName = "System.Threading.Lock+Scope";
    private const string LockThreadIdTypeName = "System.Threading.Lock+ThreadId";

    private static readonly MethodDescriptor[] AllMethods =
    [
        // Common public API
        Lock("Enter")
            .OnEnter(RecordedEventType.LockAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Lock("EnterScope")
            .Returns(Sig.ValueType(LockScopeTypeName))
            .OnEnter(RecordedEventType.LockAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Lock("Exit")
            .OnEnter(RecordedEventType.LockRelease)
            .Build(),
        TryEnter().Build(),
        TryEnter(Sig.Int32).Build(),
        TryEnter(Sig.TimeSpan).Build(),

        // Internal API
        Lock("Exit", Sig.ValueType(LockThreadIdTypeName))
            .OnRuntime(RuntimeVersions.Net9To10)
            .OnEnter(RecordedEventType.LockRelease)
            .Build(),
        Lock("Exit", Sig.Int32)
            .OnRuntime(RuntimeVersions.Net11)
            .OnEnter(RecordedEventType.LockRelease)
            .Build(),
        Lock("Wait", Sig.Int32)
            .OnRuntime(RuntimeVersions.Net11)
            .Returns(Sig.Boolean)
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.MonitorWaitAttempt)
            .OnExit(RecordedEventType.MonitorWaitResult)
            .Build(),
        Lock("Pulse")
            .OnRuntime(RuntimeVersions.Net11)
            .OnEnter(RecordedEventType.MonitorPulseOneAttempt)
            .OnExit(RecordedEventType.MonitorPulseOneResult)
            .Build(),
        Lock("PulseAll")
            .OnRuntime(RuntimeVersions.Net11)
            .OnEnter(RecordedEventType.MonitorPulseAllAttempt)
            .OnExit(RecordedEventType.MonitorPulseAllResult)
            .Build(),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static MethodDescriptorBuilder Lock(string methodName, params ArgumentTypeDescriptor[] parameterTypes)
    {
        return MethodDescriptor.ForInstanceMethod(LockTypeName, methodName)
            .OnRuntime(RuntimeVersions.Net9To11)
            .Parameters(parameterTypes)
            .CaptureThis();
    }

    private static MethodDescriptorBuilder TryEnter(params ArgumentTypeDescriptor[] parameterTypes)
    {
        return Lock("TryEnter", parameterTypes)
            .Returns(Sig.Boolean)
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.LockTryAcquire)
            .OnExit(RecordedEventType.LockAcquireResult);
    }
}
