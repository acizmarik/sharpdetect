// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Descriptors;

namespace SharpDetect.Plugins.Deadlock.Descriptors;

internal static class MonitorMethodDescriptors
{
    private static readonly MethodDescriptor _monitorEnterObject;
    private static readonly MethodDescriptor _monitorReliableEnter;
    private static readonly MethodDescriptor _monitorReliableEnterTimeout;
    private static readonly MethodDescriptor _monitorExit;
    private static readonly MethodDescriptor _monitorPulseOne;
    private static readonly MethodDescriptor _monitorPulseAll;
    private static readonly MethodDescriptor _monitorWaitObject;
    private static readonly MethodDescriptor _monitorWaitObjectInt;

    static MonitorMethodDescriptors()
    {
        _monitorEnterObject = new MethodDescriptor(
            "Enter",
            "System.Threading.Monitor",
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                CorElementType.ELEMENT_TYPE_VOID,
                [CorElementType.ELEMENT_TYPE_OBJECT]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: true,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockAcquire,
                (ushort)RecordedEventType.MonitorLockAcquireResult));

        _monitorReliableEnter = new MethodDescriptor(
            "ReliableEnter",
            "System.Threading.Monitor",
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                CorElementType.ELEMENT_TYPE_VOID,
                [
                    CorElementType.ELEMENT_TYPE_OBJECT,
                    CorElementType.ELEMENT_TYPE_BYREF,
                    CorElementType.ELEMENT_TYPE_BOOLEAN
                ]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: true,
                Arguments:
                [
                    new(0, new ((byte)nint.Size, CapturedValue.CaptureAsReference)),
                    new(1, new (1, CapturedValue.CaptureAsValue | CapturedValue.IndirectLoad))
                ],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockAcquire,
                (ushort)RecordedEventType.MonitorLockAcquireResult));

        _monitorReliableEnterTimeout = new MethodDescriptor(
            "ReliableEnterTimeout",
            "System.Threading.Monitor",
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 3,
                CorElementType.ELEMENT_TYPE_VOID,
                [
                     CorElementType.ELEMENT_TYPE_OBJECT,
                     CorElementType.ELEMENT_TYPE_I4,
                     CorElementType.ELEMENT_TYPE_BYREF,
                     CorElementType.ELEMENT_TYPE_BOOLEAN
                ]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: true,
                Arguments:
                [
                    new(0, new ((byte)nint.Size, CapturedValue.CaptureAsReference)),
                    new(2, new (1, CapturedValue.CaptureAsValue | CapturedValue.IndirectLoad))
                ],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockTryAcquire,
                (ushort)RecordedEventType.MonitorLockAcquireResult));

        _monitorExit = new MethodDescriptor(
            "Exit",
            "System.Threading.Monitor",
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                CorElementType.ELEMENT_TYPE_VOID,
                [CorElementType.ELEMENT_TYPE_OBJECT]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: true,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockRelease,
                (ushort)RecordedEventType.MonitorLockReleaseResult));

        _monitorPulseOne = new MethodDescriptor(
            "Pulse",
            "System.Threading.Monitor",
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                CorElementType.ELEMENT_TYPE_VOID,
                [CorElementType.ELEMENT_TYPE_OBJECT]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorPulseOneAttempt,
                (ushort)RecordedEventType.MonitorPulseOneResult));

        _monitorPulseAll = new MethodDescriptor(
            "PulseAll",
            "System.Threading.Monitor",
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                CorElementType.ELEMENT_TYPE_VOID,
                [CorElementType.ELEMENT_TYPE_OBJECT]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorPulseOneAttempt,
                (ushort)RecordedEventType.MonitorPulseOneResult));

        _monitorWaitObject = new MethodDescriptor(
            "Wait",
            "System.Threading.Monitor",
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                CorElementType.ELEMENT_TYPE_BOOLEAN,
                [CorElementType.ELEMENT_TYPE_OBJECT]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: new CapturedValueDescriptor(sizeof(bool), CapturedValue.CaptureAsValue),
                (ushort)RecordedEventType.MonitorWaitAttempt,
                (ushort)RecordedEventType.MonitorWaitResult));

        _monitorWaitObjectInt = new MethodDescriptor(
            "Wait",
            "System.Threading.Monitor",
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                CorElementType.ELEMENT_TYPE_BOOLEAN,
                [CorElementType.ELEMENT_TYPE_OBJECT, CorElementType.ELEMENT_TYPE_I4]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: new CapturedValueDescriptor(sizeof(bool), CapturedValue.CaptureAsValue),
                (ushort)RecordedEventType.MonitorWaitAttempt,
                (ushort)RecordedEventType.MonitorWaitResult));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        // Common public API
        yield return _monitorEnterObject;
        yield return _monitorExit;
        yield return _monitorPulseOne;
        yield return _monitorPulseAll;
        yield return _monitorWaitObject;
        yield return _monitorWaitObjectInt;

        // Internal API (usable only by BCL)
        yield return _monitorReliableEnter;
        yield return _monitorReliableEnterTimeout;
    }
}
