// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Types;

public static class SharpDetectHelperTypeDescriptors
{
    private static readonly TypeInjectionDescriptor CoreTypeDescriptor = new(
        TypeFullName: "SharpDetect",
        Methods:
        [
            new MethodInjectionDescriptor(
                Name: "ReadStaticField",
                EventType: Core.Events.RecordedEventType.StaticFieldRead,
                Signature: new MethodSignatureDescriptor(
                    CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                    ParametersCount: 1,
                    ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                    ArgumentTypeElements:
                    [
                        ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I8)
                    ])),
            new MethodInjectionDescriptor(
                Name: "WriteStaticField",
                EventType: Core.Events.RecordedEventType.StaticFieldWrite,
                Signature: new MethodSignatureDescriptor(
                    CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                    ParametersCount: 1,
                    ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                    ArgumentTypeElements:
                    [
                        ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I8)
                    ]))
        ]);
    
    public static IEnumerable<TypeInjectionDescriptor> GetAllTypes()
    {
        yield return CoreTypeDescriptor;
    }
}