// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class WaitHandleMethodDescriptors
{
    private const string WaitHandleTypeName = "System.Threading.WaitHandle";
    private const string MutexTypeName = "System.Threading.Mutex";
    private const string SemaphoreTypeName = "System.Threading.Semaphore";
    private const string EventWaitHandleTypeName = "System.Threading.EventWaitHandle";
    private const string AutoResetEventTypeName = "System.Threading.AutoResetEvent";
    private const string ManualResetEventTypeName = "System.Threading.ManualResetEvent";
    private const string EventResetModeTypeName = "System.Threading.EventResetMode";
    private const string AbandonedMutexExceptionTypeName = "System.Threading.AbandonedMutexException";
    private const string WaitSourceMapTypeName = "System.Diagnostics.Tracing.NativeRuntimeEventSource+WaitHandleWaitSourceMap";

    private static ArgumentTypeDescriptor WaitHandle => Sig.Class(WaitHandleTypeName);

    private static readonly MethodDescriptor[] AllMethods =
    [
        // Public API
        MethodDescriptor.ForInstanceMethod(MutexTypeName, ".ctor")
            .CaptureThis()
            .OnEnter(RecordedEventType.MutexCreate)
            .Build(),
        MethodDescriptor.ForInstanceMethod(MutexTypeName, ".ctor")
            .Parameters(Sig.Boolean)
            .CaptureThis()
            .OnEnter(RecordedEventType.MutexCreate)
            .Build(),
        MethodDescriptor.ForInstanceMethod(MutexTypeName, "ReleaseMutex")
            .CaptureThis()
            .OnEnter(RecordedEventType.MutexRelease)
            .OnExit(RecordedEventType.MutexReleaseResult)
            .Build(),
        MethodDescriptor.ForInstanceMethod(SemaphoreTypeName, ".ctor")
            .Parameters(Sig.Int32, Sig.Int32)
            .CaptureThis()
            .CaptureParameter(0, Capture.Int32)
            .CaptureParameter(1, Capture.Int32)
            .OnEnter(RecordedEventType.SemaphoreCreate)
            .Build(),
        MethodDescriptor.ForInstanceMethod(EventWaitHandleTypeName, ".ctor")
            .Parameters(Sig.Boolean, Sig.ValueType(EventResetModeTypeName))
            .CaptureThis()
            .CaptureParameter(0, Capture.Boolean)
            .OnEnter(RecordedEventType.EventWaitHandleCreate)
            .Build(),
        CreateResetEventConstructor(AutoResetEventTypeName, RecordedEventType.AutoResetEventCreate),
        CreateResetEventConstructor(ManualResetEventTypeName, RecordedEventType.ManualResetEventCreate),
        MethodDescriptor.ForInstanceMethod(EventWaitHandleTypeName, "Set")
            .Returns(Sig.Boolean)
            .CaptureThis()
            .OnEnter(RecordedEventType.EventWaitHandleSet)
            .OnExit(RecordedEventType.EventWaitHandleSetResult)
            .Build(),
        MethodDescriptor.ForInstanceMethod(EventWaitHandleTypeName, "Reset")
            .Returns(Sig.Boolean)
            .CaptureThis()
            .OnEnter(RecordedEventType.EventWaitHandleReset)
            .OnExit(RecordedEventType.EventWaitHandleResetResult)
            .Build(),
        MethodDescriptor.ForInstanceMethod(AbandonedMutexExceptionTypeName, ".ctor")
            .CaptureThis()
            .OnEnter(RecordedEventType.AbandonedMutexExceptionCreate)
            .Build(),
        MethodDescriptor.ForInstanceMethod(AbandonedMutexExceptionTypeName, ".ctor")
            .Parameters(Sig.Int32, WaitHandle)
            .CaptureThis()
            .CaptureParameter(1, Capture.Reference)
            .OnEnter(RecordedEventType.AbandonedMutexExceptionCreate)
            .Build(),

        // Internal API
        CreateWaitOneNoCheck(RuntimeVersions.Net8, Sig.Int32),
        CreateWaitOneNoCheck(
            RuntimeVersions.Net9To10,
            Sig.Int32,
            Sig.Boolean,
            Sig.Object,
            Sig.ValueType(WaitSourceMapTypeName)),
        MethodDescriptor.ForInstanceMethod(SemaphoreTypeName, "ReleaseCore")
            .Parameters(Sig.Int32)
            .Returns(Sig.Int32)
            .CaptureThis()
            .CaptureParameter(0, Capture.Int32)
            .OnEnter(RecordedEventType.SemaphoreRelease)
            .OnExit(RecordedEventType.SemaphoreReleaseResult)
            .Build(),
        MethodDescriptor.ForStaticMethod(WaitHandleTypeName, "SignalAndWait")
            .Parameters(WaitHandle, WaitHandle, Sig.Int32)
            .Returns(Sig.Boolean)
            .CaptureParameter(0, Capture.Reference)
            .CaptureParameter(1, Capture.Reference)
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.WaitHandleSignalAndWait)
            .OnExit(RecordedEventType.WaitHandleSignalAndWaitResult)
            .Build(),
        MethodDescriptor.ForStaticMethod(WaitHandleTypeName, "WaitMultiple")
            .Parameters(Sig.ArrayOf(WaitHandle), Sig.Boolean, Sig.Int32)
            .Returns(Sig.Int32)
            .CaptureParameter(0, Capture.ReferenceArray)
            .CaptureParameter(1, Capture.Boolean)
            .CaptureReturnValue(Capture.Int32)
            .OnEnter(RecordedEventType.WaitHandleWaitMultiple)
            .OnExit(RecordedEventType.WaitHandleWaitMultipleResult)
            .Build(),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static MethodDescriptor CreateResetEventConstructor(string typeName, RecordedEventType enterInterpretation)
    {
        return MethodDescriptor.ForInstanceMethod(typeName, ".ctor")
            .Parameters(Sig.Boolean)
            .CaptureThis()
            .CaptureParameter(0, Capture.Boolean)
            .OnEnter(enterInterpretation)
            .Build();
    }

    private static MethodDescriptor CreateWaitOneNoCheck(
        MethodVersionDescriptor version,
        params ArgumentTypeDescriptor[] parameterTypes)
    {
        return MethodDescriptor.ForInstanceMethod(WaitHandleTypeName, "WaitOneNoCheck")
            .OnRuntime(version)
            .Parameters(parameterTypes)
            .Returns(Sig.Boolean)
            .CaptureThis()
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.WaitHandleWait)
            .OnExit(RecordedEventType.WaitHandleWaitResult)
            .Build();
    }
}
