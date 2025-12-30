// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors;

internal static class ThreadMethodDescriptors
{
    private static readonly MethodDescriptor ThreadJoinInt32;
    private static readonly MethodDescriptor ThreadStartCallback;
    private static readonly MethodDescriptor ThreadGetCurrentThread;

    static ThreadMethodDescriptors()
    {
        const string typeFullName = "System.Threading.Thread";
        
        ThreadJoinInt32 = new MethodDescriptor(
            MethodName: "Join",
            DeclaringTypeFullName: typeFullName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: [ ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4) ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: true,
                Arguments: [
                    new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference)),
                    new(1, new(sizeof(int), CapturedValue.CaptureAsValue))],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ThreadJoinAttempt,
                MethodExitInterpretation: (ushort)RecordedEventType.ThreadJoinResult));

        ThreadStartCallback = new MethodDescriptor(
            MethodName: "StartCallback",
            DeclaringTypeFullName: typeFullName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference)) ],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ThreadStart,
                MethodExitInterpretation: null));
        
        ThreadGetCurrentThread = new MethodDescriptor(
            MethodName: "get_CurrentThread",
            DeclaringTypeFullName: typeFullName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateClass(typeFullName),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [],
                ReturnValue: new CapturedValueDescriptor((byte)nint.Size, CapturedValue.CaptureAsReference),
                MethodEnterInterpretation: null,
                MethodExitInterpretation: (ushort)RecordedEventType.ThreadMapping));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        // Common public API
        yield return ThreadJoinInt32;
        yield return ThreadGetCurrentThread;
        
        // Internal runtime API
        yield return ThreadStartCallback;
    }
}