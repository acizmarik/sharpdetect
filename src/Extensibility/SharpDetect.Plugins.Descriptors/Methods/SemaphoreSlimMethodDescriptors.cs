// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class SemaphoreSlimMethodDescriptors
{
    private const string SemaphoreSlimTypeName = "System.Threading.SemaphoreSlim";
    private const string CancellationTokenTypeName = "System.Threading.CancellationToken";
    private const string TaskTResultTypeName = "System.Threading.Tasks.Task`1";

    private static readonly Version Version8 = new(8, 0, 0);
    private static readonly Version Version10 = new(10, 0, 0);
    private static readonly Version Version10Max = new(10, int.MaxValue, int.MaxValue);
    
    private static readonly CapturedArgumentDescriptor ObjectRefArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor ReleaseCountArg =
        new(1, new(sizeof(int), CapturedValue.CaptureAsValue));
    
    private static readonly CapturedArgumentDescriptor InitialCountArg =
        new(1, new(sizeof(int), CapturedValue.CaptureAsValue));
    
    private static readonly CapturedArgumentDescriptor MaximumCountArg =
        new(2, new(sizeof(int), CapturedValue.CaptureAsValue));

    private static readonly MethodDescriptor CtorWithMaxCount;
    private static readonly MethodDescriptor WaitV8;
    private static readonly MethodDescriptor WaitCoreV10;
    private static readonly MethodDescriptor WaitAsyncV8;
    private static readonly MethodDescriptor WaitAsyncCoreV10;
    private static readonly MethodDescriptor ReleaseWithCount;

    static SemaphoreSlimMethodDescriptors()
    {
        CtorWithMaxCount = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: SemaphoreSlimTypeName,
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

        WaitV8 = new MethodDescriptor(
            MethodName: "Wait",
            DeclaringTypeFullName: SemaphoreSlimTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version8, Version10),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                    ArgumentTypeDescriptor.CreateValueType(CancellationTokenTypeName)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: new CapturedValueDescriptor(sizeof(bool), CapturedValue.CaptureAsValue),
                MethodEnterInterpretation: (ushort)RecordedEventType.SemaphoreTryAcquire,
                MethodExitInterpretation: (ushort)RecordedEventType.SemaphoreAcquireResult));
        
        WaitCoreV10 = new MethodDescriptor(
            MethodName: "WaitCore",
            DeclaringTypeFullName: SemaphoreSlimTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version10, Version10Max),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I8),
                    ArgumentTypeDescriptor.CreateValueType(CancellationTokenTypeName)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: new CapturedValueDescriptor(sizeof(bool), CapturedValue.CaptureAsValue),
                MethodEnterInterpretation: (ushort)RecordedEventType.SemaphoreTryAcquire,
                MethodExitInterpretation: (ushort)RecordedEventType.SemaphoreAcquireResult));

        WaitAsyncV8 = new MethodDescriptor(
            MethodName: "WaitAsync",
            DeclaringTypeFullName: SemaphoreSlimTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version8, Version10),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateGenericInst(
                    TaskTResultTypeName,
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN)),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                    ArgumentTypeDescriptor.CreateValueType(CancellationTokenTypeName)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: new CapturedValueDescriptor((byte)nint.Size, CapturedValue.CaptureAsReference),
                MethodEnterInterpretation: (ushort)RecordedEventType.SemaphoreWaitAsync,
                MethodExitInterpretation: (ushort)RecordedEventType.SemaphoreWaitAsyncResult));

        WaitAsyncCoreV10 = new MethodDescriptor(
            MethodName: "WaitAsyncCore",
            DeclaringTypeFullName: SemaphoreSlimTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version10, Version10Max),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateGenericInst(
                    TaskTResultTypeName,
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN)),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I8),
                    ArgumentTypeDescriptor.CreateValueType(CancellationTokenTypeName)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: new CapturedValueDescriptor((byte)nint.Size, CapturedValue.CaptureAsReference),
                MethodEnterInterpretation: (ushort)RecordedEventType.SemaphoreWaitAsync,
                MethodExitInterpretation: (ushort)RecordedEventType.SemaphoreWaitAsyncResult));

        ReleaseWithCount = new MethodDescriptor(
            MethodName: "Release",
            DeclaringTypeFullName: SemaphoreSlimTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ ObjectRefArg, ReleaseCountArg ],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.SemaphoreRelease,
                MethodExitInterpretation: (ushort)RecordedEventType.SemaphoreReleaseResult));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        // Public API
        yield return CtorWithMaxCount;
        yield return ReleaseWithCount;

        // Internal API
        yield return WaitV8;
        yield return WaitCoreV10;
        yield return WaitAsyncV8;
        yield return WaitAsyncCoreV10;
    }
}

