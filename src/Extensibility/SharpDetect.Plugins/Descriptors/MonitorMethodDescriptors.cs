// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors;

internal static class MonitorMethodDescriptors
{
    private static readonly MethodDescriptor _monitorEnterObject_v8;
    private static readonly MethodDescriptor _monitorEnterObject_v10;
    private static readonly MethodDescriptor _monitorEnterObjectLockTaken_v10;
    private static readonly MethodDescriptor _monitorReliableEnter_v8;
    private static readonly MethodDescriptor _monitorReliableEnterTimeout_v8;
    private static readonly MethodDescriptor _monitorTryEnterObject_v10;
    private static readonly MethodDescriptor _monitorTryEnterTimeoutObject_v10;
    private static readonly MethodDescriptor _monitorTryReliableEnterObject_v10;
    private static readonly MethodDescriptor _monitorTryReliableEnterObjectTimeout_v10;
    private static readonly MethodDescriptor _monitorExit_v8;
    private static readonly MethodDescriptor _monitorExit_v10;
    private static readonly MethodDescriptor _monitorPulseOne;
    private static readonly MethodDescriptor _monitorPulseAll;
    private static readonly MethodDescriptor _monitorWaitObject;
    private static readonly MethodDescriptor _monitorWaitObjectInt;

    static MonitorMethodDescriptors()
    {
        _monitorEnterObject_v8 = new MethodDescriptor(
            "Enter",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(8, 0, 0), new Version(9, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: true,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockAcquire,
                (ushort)RecordedEventType.MonitorLockAcquireResult));
        
        _monitorEnterObject_v10 = new MethodDescriptor(
            "Enter",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(10, 0, 0), new Version(10, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockAcquire,
                (ushort)RecordedEventType.MonitorLockAcquireResult));

        _monitorEnterObjectLockTaken_v10 = new MethodDescriptor(
            "Enter",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(10, 0, 0), new Version(10, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
                    ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))
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

        _monitorReliableEnter_v8 = new MethodDescriptor(
            "ReliableEnter",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(8, 0, 0), new Version(9, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
                    ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))
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

        _monitorReliableEnterTimeout_v8 = new MethodDescriptor(
            "ReliableEnterTimeout",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(8, 0, 0), new Version(9, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 3,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [
                     ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
                     ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                     ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))
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

        _monitorTryEnterObject_v10 = new MethodDescriptor(
            "TryEnter",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(10, 0, 0), new Version(10, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)
                ]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments:
                [
                    new(0, new ((byte)nint.Size, CapturedValue.CaptureAsReference))
                ],
                ReturnValue: new CapturedValueDescriptor(sizeof(bool), CapturedValue.CaptureAsValue),
                (ushort)RecordedEventType.MonitorLockTryAcquire,
                (ushort)RecordedEventType.MonitorLockAcquireResult));
        
        _monitorTryEnterTimeoutObject_v10 = new MethodDescriptor(
            "TryEnter",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(10, 0, 0), new Version(10, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)
                ]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments:
                [
                    new(0, new ((byte)nint.Size, CapturedValue.CaptureAsReference)),
                ],
                ReturnValue: new CapturedValueDescriptor(sizeof(bool), CapturedValue.CaptureAsValue),
                (ushort)RecordedEventType.MonitorLockTryAcquire,
                (ushort)RecordedEventType.MonitorLockAcquireResult));
        
        _monitorTryReliableEnterObject_v10 = new MethodDescriptor(
            "TryEnter",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(10, 0, 0), new Version(10, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
                    ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))
                ]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments:
                [
                    new(0, new ((byte)nint.Size, CapturedValue.CaptureAsReference)),
                    new(1, new (1, CapturedValue.CaptureAsValue | CapturedValue.IndirectLoad))
                ],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockTryAcquire,
                (ushort)RecordedEventType.MonitorLockAcquireResult));
        
        _monitorTryReliableEnterObjectTimeout_v10 = new MethodDescriptor(
            "TryEnter",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(10, 0, 0), new Version(10, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 3,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                    ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))
                ]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments:
                [
                    new(0, new ((byte)nint.Size, CapturedValue.CaptureAsReference)),
                    new(2, new (sizeof(bool), CapturedValue.CaptureAsValue | CapturedValue.IndirectLoad))
                ],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockTryAcquire,
                (ushort)RecordedEventType.MonitorLockAcquireResult));
        
        _monitorExit_v8 = new MethodDescriptor(
            "Exit",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(8, 0, 0), new Version(9, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: true,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockRelease,
                (ushort)RecordedEventType.MonitorLockReleaseResult));
        
        _monitorExit_v10 = new MethodDescriptor(
            "Exit",
            "System.Threading.Monitor",
            MethodVersionDescriptor.Create(new Version(10, 0, 0), new Version(10, int.MaxValue, int.MaxValue)),
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorLockRelease,
                (ushort)RecordedEventType.MonitorLockReleaseResult));

        _monitorPulseOne = new MethodDescriptor(
            "Pulse",
            "System.Threading.Monitor",
            VersionDescriptor: null,
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)]),
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
            VersionDescriptor: null,
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)]),
            new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference))],
                ReturnValue: null,
                (ushort)RecordedEventType.MonitorPulseAllAttempt,
                (ushort)RecordedEventType.MonitorPulseAllResult));

        _monitorWaitObject = new MethodDescriptor(
            "Wait",
            "System.Threading.Monitor",
            VersionDescriptor: null,
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)]),
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
            VersionDescriptor: null,
            new MethodSignatureDescriptor(
                CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT), ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)]),
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
        yield return _monitorEnterObject_v8;
        yield return _monitorEnterObject_v10;
        yield return _monitorEnterObjectLockTaken_v10;
        yield return _monitorTryEnterObject_v10;
        yield return _monitorTryEnterTimeoutObject_v10;
        yield return _monitorTryReliableEnterObject_v10;
        yield return _monitorTryReliableEnterObjectTimeout_v10;
        yield return _monitorExit_v8;
        yield return _monitorExit_v10;
        yield return _monitorPulseOne;
        yield return _monitorPulseAll;
        yield return _monitorWaitObject;
        yield return _monitorWaitObjectInt;

        // Internal API (usable only by BCL)
        yield return _monitorReliableEnter_v8;
        yield return _monitorReliableEnterTimeout_v8;
    }
}

