// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

// FIXME: generic method overloads on Interlocked are not yet supported
public static class VolatileMethodDescriptors
{
    private const string VolatileTypeName = "System.Threading.Volatile";

    private static readonly CorElementType[] PrimitiveTypes =
    [
        CorElementType.ELEMENT_TYPE_BOOLEAN,
        CorElementType.ELEMENT_TYPE_I1,
        CorElementType.ELEMENT_TYPE_U1,
        CorElementType.ELEMENT_TYPE_I2,
        CorElementType.ELEMENT_TYPE_U2,
        CorElementType.ELEMENT_TYPE_I4,
        CorElementType.ELEMENT_TYPE_U4,
        CorElementType.ELEMENT_TYPE_I8,
        CorElementType.ELEMENT_TYPE_U8,
        CorElementType.ELEMENT_TYPE_R4,
        CorElementType.ELEMENT_TYPE_R8,
        CorElementType.ELEMENT_TYPE_I,
        CorElementType.ELEMENT_TYPE_U
    ];

    private static readonly MethodDescriptor[] AllMethods =
    [
        .. PrimitiveTypes.Select(CreateRead),
        .. PrimitiveTypes.Select(CreateWrite),
    ];

    private static MethodDescriptor CreateRead(CorElementType elementType)
    {
        var valueType = ArgumentTypeDescriptor.CreateSimple(elementType);
        return Create(
            methodName: "Read",
            signature: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 1,
                ReturnType: valueType,
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateByRef(valueType)]),
            interpretation: FieldAddressAccessInterpretation.VolatileRead);
    }

    private static MethodDescriptor CreateWrite(CorElementType elementType)
    {
        var valueType = ArgumentTypeDescriptor.CreateSimple(elementType);
        return Create(
            methodName: "Write",
            signature: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateByRef(valueType), valueType]),
            interpretation: FieldAddressAccessInterpretation.VolatileWrite);
    }

    private static MethodDescriptor Create(
        string methodName,
        MethodSignatureDescriptor signature,
        FieldAddressAccessInterpretation interpretation)
    {
        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: VolatileTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: signature,
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: false,
                InjectManagedWrapper: false,
                Arguments: [],
                ReturnValue: null,
                MethodEnterInterpretation: null,
                MethodExitInterpretation: null,
                FieldAddressAccessInterpretation: interpretation));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;
}
