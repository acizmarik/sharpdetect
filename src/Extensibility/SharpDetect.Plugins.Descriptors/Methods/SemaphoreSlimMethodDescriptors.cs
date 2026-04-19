// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class SemaphoreSlimMethodDescriptors
{
    private const string SemaphoreSlimTypeName = "System.Threading.SemaphoreSlim";
    private const string CancellationTokenTypeName = "System.Threading.CancellationToken";

    private static readonly Version Version8 = new(8, 0, 0);
    private static readonly Version Version10 = new(10, 0, 0);
    private static readonly Version Version10Max = new(10, int.MaxValue, int.MaxValue);
    
    private static readonly CapturedArgumentDescriptor ObjectRefArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));
    
    private static readonly MethodDescriptor WaitV8;
    private static readonly MethodDescriptor WaitCoreV10;
    private static readonly MethodDescriptor ReleaseNoArgs;
    private static readonly MethodDescriptor ReleaseWithCount;

    static SemaphoreSlimMethodDescriptors()
    {
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

        ReleaseNoArgs = new MethodDescriptor(
            MethodName: "Release",
            DeclaringTypeFullName: SemaphoreSlimTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.SemaphoreRelease,
                MethodExitInterpretation: (ushort)RecordedEventType.SemaphoreReleaseResult));

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
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.SemaphoreRelease,
                MethodExitInterpretation: (ushort)RecordedEventType.SemaphoreReleaseResult));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        // Internal API
        yield return WaitV8;
        yield return WaitCoreV10;
        
        // Public API
        yield return ReleaseNoArgs;
        yield return ReleaseWithCount;
    }
}

