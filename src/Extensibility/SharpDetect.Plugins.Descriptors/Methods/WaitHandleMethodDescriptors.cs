// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

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

    private static readonly Version Version8 = new(8, 0, 0);
    private static readonly Version Version8Max = new(8, int.MaxValue, int.MaxValue);
    private static readonly Version Version9 = new(9, 0, 0);
    private static readonly Version Version10Max = new(10, int.MaxValue, int.MaxValue);

    private static readonly CapturedArgumentDescriptor ObjectRefArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor SecondObjectRefArg =
        new(1, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor AbandonedMutexHandleArg =
        new(2, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor WaitHandlesArrayArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReferenceArray));

    private static readonly CapturedArgumentDescriptor WaitAllArg =
        new(1, new(sizeof(bool), CapturedValue.CaptureAsValue));

    private static readonly CapturedArgumentDescriptor InitialStateArg =
        new(1, new(sizeof(bool), CapturedValue.CaptureAsValue));

    private static readonly CapturedArgumentDescriptor ReleaseCountArg =
        new(1, new(sizeof(int), CapturedValue.CaptureAsValue));

    private static readonly CapturedArgumentDescriptor InitialCountArg =
        new(1, new(sizeof(int), CapturedValue.CaptureAsValue));

    private static readonly CapturedArgumentDescriptor MaximumCountArg =
        new(2, new(sizeof(int), CapturedValue.CaptureAsValue));

    private static readonly MethodDescriptor WaitOneNoCheckV8;
    private static readonly MethodDescriptor WaitOneNoCheckV9;
    private static readonly MethodDescriptor MutexCtorParameterless;
    private static readonly MethodDescriptor MutexCtor;
    private static readonly MethodDescriptor MutexRelease;
    private static readonly MethodDescriptor SemaphoreCtor;
    private static readonly MethodDescriptor SemaphoreReleaseCore;
    private static readonly MethodDescriptor EventWaitHandleCtor;
    private static readonly MethodDescriptor AutoResetEventCtor;
    private static readonly MethodDescriptor ManualResetEventCtor;
    private static readonly MethodDescriptor EventWaitHandleSet;
    private static readonly MethodDescriptor EventWaitHandleReset;
    private static readonly MethodDescriptor SignalAndWait;
    private static readonly MethodDescriptor WaitMultiple;
    private static readonly MethodDescriptor AbandonedMutexExceptionCtorParameterless;
    private static readonly MethodDescriptor AbandonedMutexExceptionCtorWithHandle;

    static WaitHandleMethodDescriptors()
    {
        WaitOneNoCheckV8 = new MethodDescriptor(
            MethodName: "WaitOneNoCheck",
            DeclaringTypeFullName: WaitHandleTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version8, Version8Max),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: new CapturedValueDescriptor(sizeof(bool), CapturedValue.CaptureAsValue),
                MethodEnterInterpretation: (ushort)RecordedEventType.WaitHandleWait,
                MethodExitInterpretation: (ushort)RecordedEventType.WaitHandleWaitResult));

        WaitOneNoCheckV9 = new MethodDescriptor(
            MethodName: "WaitOneNoCheck",
            DeclaringTypeFullName: WaitHandleTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version9, Version10Max),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 4,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
                    ArgumentTypeDescriptor.CreateValueType(WaitSourceMapTypeName)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: new CapturedValueDescriptor(sizeof(bool), CapturedValue.CaptureAsValue),
                MethodEnterInterpretation: (ushort)RecordedEventType.WaitHandleWait,
                MethodExitInterpretation: (ushort)RecordedEventType.WaitHandleWaitResult));
        
        MutexCtorParameterless = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: MutexTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.MutexCreate,
                MethodExitInterpretation: null));

        MutexCtor = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: MutexTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.MutexCreate,
                MethodExitInterpretation: null));

        MutexRelease = new MethodDescriptor(
            MethodName: "ReleaseMutex",
            DeclaringTypeFullName: MutexTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.MutexRelease,
                MethodExitInterpretation: (ushort)RecordedEventType.MutexReleaseResult));

        SemaphoreCtor = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: SemaphoreTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg, InitialCountArg, MaximumCountArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.SemaphoreCreate,
                MethodExitInterpretation: null));

        SemaphoreReleaseCore = new MethodDescriptor(
            MethodName: "ReleaseCore",
            DeclaringTypeFullName: SemaphoreTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg, ReleaseCountArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.SemaphoreRelease,
                MethodExitInterpretation: (ushort)RecordedEventType.SemaphoreReleaseResult));
        
        EventWaitHandleCtor = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: EventWaitHandleTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                    ArgumentTypeDescriptor.CreateValueType(EventResetModeTypeName)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg, InitialStateArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.EventWaitHandleCreate,
                MethodExitInterpretation: null));

        AutoResetEventCtor = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: AutoResetEventTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg, InitialStateArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.AutoResetEventCreate,
                MethodExitInterpretation: null));

        ManualResetEventCtor = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: ManualResetEventTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg, InitialStateArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ManualResetEventCreate,
                MethodExitInterpretation: null));

        EventWaitHandleSet = new MethodDescriptor(
            MethodName: "Set",
            DeclaringTypeFullName: EventWaitHandleTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.EventWaitHandleSet,
                MethodExitInterpretation: (ushort)RecordedEventType.EventWaitHandleSetResult));

        EventWaitHandleReset = new MethodDescriptor(
            MethodName: "Reset",
            DeclaringTypeFullName: EventWaitHandleTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.EventWaitHandleReset,
                MethodExitInterpretation: (ushort)RecordedEventType.EventWaitHandleResetResult));

        SignalAndWait = new MethodDescriptor(
            MethodName: "SignalAndWait",
            DeclaringTypeFullName: WaitHandleTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 3,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateClass(WaitHandleTypeName),
                    ArgumentTypeDescriptor.CreateClass(WaitHandleTypeName),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg, SecondObjectRefArg],
                ReturnValue: new CapturedValueDescriptor(sizeof(bool), CapturedValue.CaptureAsValue),
                MethodEnterInterpretation: (ushort)RecordedEventType.WaitHandleSignalAndWait,
                MethodExitInterpretation: (ushort)RecordedEventType.WaitHandleSignalAndWaitResult));

        WaitMultiple = new MethodDescriptor(
            MethodName: "WaitMultiple",
            DeclaringTypeFullName: WaitHandleTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 3,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSZArray(ArgumentTypeDescriptor.CreateClass(WaitHandleTypeName)),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [WaitHandlesArrayArg, WaitAllArg],
                ReturnValue: new CapturedValueDescriptor(sizeof(int), CapturedValue.CaptureAsValue),
                MethodEnterInterpretation: (ushort)RecordedEventType.WaitHandleWaitMultiple,
                MethodExitInterpretation: (ushort)RecordedEventType.WaitHandleWaitMultipleResult));

        AbandonedMutexExceptionCtorParameterless = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: AbandonedMutexExceptionTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.AbandonedMutexExceptionCreate,
                MethodExitInterpretation: null));

        AbandonedMutexExceptionCtorWithHandle = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: AbandonedMutexExceptionTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                    ArgumentTypeDescriptor.CreateClass(WaitHandleTypeName)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg, AbandonedMutexHandleArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.AbandonedMutexExceptionCreate,
                MethodExitInterpretation: null));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        // Public API
        yield return MutexCtorParameterless;
        yield return MutexCtor;
        yield return MutexRelease;
        yield return SemaphoreCtor;
        yield return EventWaitHandleCtor;
        yield return AutoResetEventCtor;
        yield return ManualResetEventCtor;
        yield return EventWaitHandleSet;
        yield return EventWaitHandleReset;
        yield return AbandonedMutexExceptionCtorParameterless;
        yield return AbandonedMutexExceptionCtorWithHandle;

        // Internal API
        yield return WaitOneNoCheckV8;
        yield return WaitOneNoCheckV9;
        yield return SemaphoreReleaseCore;
        yield return SignalAndWait;
        yield return WaitMultiple;
    }
}
