// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class MonitorMethodDescriptors
{
    private const string MonitorTypeName = "System.Threading.Monitor";

    private static readonly MethodDescriptor[] AllMethods =
    [
        // .NET 8-9: common public API
        Monitor("Enter", Sig.Object)
            .OnRuntime(RuntimeVersions.Net8To9)
            .WithManagedWrapper()
            .OnEnter(RecordedEventType.LockAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Monitor("Enter", Sig.Object, Sig.ByRef(Sig.Boolean))
            .OnRuntime(RuntimeVersions.Net8To9)
            .WithManagedWrapper()
            .CaptureParameter(1, Capture.OutBoolean)
            .OnEnter(RecordedEventType.LockAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),

        // .NET 8-9: internal API
        Monitor("ReliableEnterTimeout", Sig.Object, Sig.Int32, Sig.ByRef(Sig.Boolean))
            .OnRuntime(RuntimeVersions.Net8To9)
            .WithManagedWrapper()
            .CaptureParameter(2, Capture.OutBoolean)
            .OnEnter(RecordedEventType.LockTryAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Monitor("Exit", Sig.Object)
            .OnRuntime(RuntimeVersions.Net8To9)
            .WithManagedWrapper()
            .OnEnter(RecordedEventType.LockRelease)
            .Build(),

        // .NET 10-11: common public API
        Monitor("Enter", Sig.Object)
            .OnRuntime(RuntimeVersions.Net10To11)
            .OnEnter(RecordedEventType.LockAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Monitor("Enter", Sig.Object, Sig.ByRef(Sig.Boolean))
            .OnRuntime(RuntimeVersions.Net10)
            .CaptureParameter(1, Capture.OutBoolean)
            .OnEnter(RecordedEventType.LockAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Monitor("TryEnter", Sig.Object)
            .OnRuntime(RuntimeVersions.Net10To11)
            .Returns(Sig.Boolean)
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.LockTryAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Monitor("TryEnter", Sig.Object, Sig.Int32)
            .OnRuntime(RuntimeVersions.Net10To11)
            .Returns(Sig.Boolean)
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.LockTryAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Monitor("TryEnter", Sig.Object, Sig.ByRef(Sig.Boolean))
            .OnRuntime(RuntimeVersions.Net10)
            .CaptureParameter(1, Capture.OutBoolean)
            .OnEnter(RecordedEventType.LockTryAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Monitor("TryEnter", Sig.Object, Sig.Int32, Sig.ByRef(Sig.Boolean))
            .OnRuntime(RuntimeVersions.Net10)
            .CaptureParameter(2, Capture.OutBoolean)
            .OnEnter(RecordedEventType.LockTryAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Monitor("Exit", Sig.Object)
            .OnRuntime(RuntimeVersions.Net10To11)
            .OnEnter(RecordedEventType.LockRelease)
            .Build(),
        Monitor("ExitIfLockTaken", Sig.Object, Sig.ByRef(Sig.Boolean))
            .OnRuntime(RuntimeVersions.Net10)
            .CaptureParameter(1, Capture.DereferencedBoolean)
            .OnEnter(RecordedEventType.LockRelease)
            .Build(),

        Monitor("SynchronizedMethodEnter", Sig.Object, Sig.ByRef(Sig.Boolean))
            .OnRuntime(RuntimeVersions.Net11)
            .CaptureParameter(1, Capture.OutBoolean)
            .OnEnter(RecordedEventType.LockAcquire)
            .OnExit(RecordedEventType.LockAcquireResult)
            .Build(),
        Monitor("SynchronizedMethodExit", Sig.Object, Sig.ByRef(Sig.Boolean))
            .OnRuntime(RuntimeVersions.Net11)
            .CaptureParameter(1, Capture.DereferencedBoolean)
            .OnEnter(RecordedEventType.LockRelease)
            .Build(),

        // All runtimes: common public API
        Monitor("Pulse", Sig.Object)
            .OnEnter(RecordedEventType.MonitorPulseOneAttempt)
            .OnExit(RecordedEventType.MonitorPulseOneResult)
            .Build(),
        Monitor("PulseAll", Sig.Object)
            .OnEnter(RecordedEventType.MonitorPulseAllAttempt)
            .OnExit(RecordedEventType.MonitorPulseAllResult)
            .Build(),
        Monitor("Wait", Sig.Object, Sig.Int32)
            .Returns(Sig.Boolean)
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.MonitorWaitAttempt)
            .OnExit(RecordedEventType.MonitorWaitResult)
            .Build(),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static MethodDescriptorBuilder Monitor(string methodName, params ArgumentTypeDescriptor[] parameterTypes)
    {
        return MethodDescriptor.ForStaticMethod(MonitorTypeName, methodName)
            .Parameters(parameterTypes)
            .CaptureParameter(0, Capture.Reference);
    }
}
