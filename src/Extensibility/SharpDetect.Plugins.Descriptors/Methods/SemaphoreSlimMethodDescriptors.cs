// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class SemaphoreSlimMethodDescriptors
{
    private const string SemaphoreSlimTypeName = "System.Threading.SemaphoreSlim";
    private const string TaskTResultTypeName = "System.Threading.Tasks.Task`1";

    private static readonly MethodDescriptor[] AllMethods =
    [
        // Public API
        MethodDescriptor.ForInstanceMethod(SemaphoreSlimTypeName, ".ctor")
            .Parameters(Sig.Int32, Sig.Int32)
            .CaptureThis()
            .CaptureParameter(0, Capture.Int32)
            .CaptureParameter(1, Capture.Int32)
            .OnEnter(RecordedEventType.SemaphoreCreate)
            .Build(),
        MethodDescriptor.ForInstanceMethod(SemaphoreSlimTypeName, "Release")
            .Parameters(Sig.Int32)
            .Returns(Sig.Int32)
            .CaptureThis()
            .CaptureParameter(0, Capture.Int32)
            .OnEnter(RecordedEventType.SemaphoreRelease)
            .OnExit(RecordedEventType.SemaphoreReleaseResult)
            .Build(),

        // Internal API
        CreateWait("Wait", RuntimeVersions.Net8To9, Sig.Int32),
        CreateWait("WaitCore", RuntimeVersions.Net10, Sig.Int64),
        CreateWaitAsync("WaitAsync", RuntimeVersions.Net8To9, Sig.Int32),
        CreateWaitAsync("WaitAsyncCore", RuntimeVersions.Net10, Sig.Int64),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static MethodDescriptor CreateWait(
        string methodName,
        MethodVersionDescriptor version,
        ArgumentTypeDescriptor timeoutType)
    {
        return MethodDescriptor.ForInstanceMethod(SemaphoreSlimTypeName, methodName)
            .OnRuntime(version)
            .Parameters(timeoutType, Sig.CancellationToken)
            .Returns(Sig.Boolean)
            .CaptureThis()
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.SemaphoreTryAcquire)
            .OnExit(RecordedEventType.SemaphoreAcquireResult)
            .Build();
    }

    private static MethodDescriptor CreateWaitAsync(
        string methodName,
        MethodVersionDescriptor version,
        ArgumentTypeDescriptor timeoutType)
    {
        return MethodDescriptor.ForInstanceMethod(SemaphoreSlimTypeName, methodName)
            .OnRuntime(version)
            .Parameters(timeoutType, Sig.CancellationToken)
            .Returns(Sig.Generic(TaskTResultTypeName, Sig.Boolean))
            .CaptureThis()
            .CaptureReturnValue(Capture.Reference)
            .OnEnter(RecordedEventType.SemaphoreWaitAsync)
            .OnExit(RecordedEventType.SemaphoreWaitAsyncResult)
            .Build();
    }
}
