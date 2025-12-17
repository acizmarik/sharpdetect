// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors;

internal static class LockMethodDescriptors
{
    private const string LockTypeName = "System.Threading.Lock";
    private static readonly Version Version9 = new(9, 0, 0);
    private static readonly Version Version10Max = new(10, int.MaxValue, int.MaxValue);
    private static readonly CapturedArgumentDescriptor ObjectRefArg = 
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly MethodDescriptor Enter;
    private static readonly MethodDescriptor Exit;
    private static readonly MethodDescriptor TryEnter;
    private static readonly MethodDescriptor TryEnterTimeout;

    static LockMethodDescriptors()
    {
        Enter = new MethodDescriptor(
            MethodName: "Enter",
            DeclaringTypeFullName: LockTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version9, Version10Max),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ ObjectRefArg ],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.LockAcquire,
                MethodExitInterpretation: (ushort)RecordedEventType.LockAcquireResult));
        
        Exit = new MethodDescriptor(
            MethodName: "Exit",
            DeclaringTypeFullName: LockTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version9, Version10Max),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ ObjectRefArg ],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.LockRelease,
                MethodExitInterpretation: (ushort)RecordedEventType.LockReleaseResult));
        
        TryEnter = new MethodDescriptor(
            MethodName: "TryEnter",
            DeclaringTypeFullName: LockTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version9, Version10Max),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ ObjectRefArg ],
                ReturnValue: new(sizeof(bool), CapturedValue.CaptureAsValue),
                MethodEnterInterpretation: (ushort)RecordedEventType.LockTryAcquire,
                MethodExitInterpretation: (ushort)RecordedEventType.LockAcquireResult));
        
        TryEnterTimeout = new MethodDescriptor(
            MethodName: "TryEnter",
            DeclaringTypeFullName: LockTypeName,
            VersionDescriptor: MethodVersionDescriptor.Create(Version9, Version10Max),
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: [ ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4) ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ ObjectRefArg ],
                ReturnValue: new(sizeof(bool), CapturedValue.CaptureAsValue),
                MethodEnterInterpretation: (ushort)RecordedEventType.LockTryAcquire,
                MethodExitInterpretation: (ushort)RecordedEventType.LockAcquireResult));
    }
    
    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        // Common public API
        yield return Enter;
        yield return Exit;
        yield return TryEnter;
        yield return TryEnterTimeout;
    }
}

