// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class FieldAccessDescriptors
{
    private static readonly MethodDescriptor ReadStaticField;
    private static readonly MethodDescriptor WriteStaticField;

    static FieldAccessDescriptors()
    {
        const string typeFullName = "SharpDetect";
        
        ReadStaticField = new MethodDescriptor(
            MethodName: "ReadStaticField",
            DeclaringTypeFullName: typeFullName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [ ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I8) ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments:
                [
                    new(0, new(sizeof(ulong), CapturedValue.CaptureAsValue))
                ],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.StaticFieldRead,
                MethodExitInterpretation: null));

        WriteStaticField = new MethodDescriptor(
            MethodName: "WriteStaticField",
            DeclaringTypeFullName: typeFullName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [ ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I8) ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments:
                [
                    new(0, new(sizeof(ulong), CapturedValue.CaptureAsValue))
                ],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.StaticFieldWrite,
                MethodExitInterpretation: null));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return ReadStaticField;
        yield return WriteStaticField;
    }
}