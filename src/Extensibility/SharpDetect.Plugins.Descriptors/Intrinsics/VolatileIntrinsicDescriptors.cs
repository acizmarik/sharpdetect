// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Intrinsics;

public static class VolatileIntrinsicDescriptors
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

    private static readonly FieldAccessIntrinsicDescriptor[] AllIntrinsics =
    [
        .. PrimitiveTypes.Select(type => CreateRead(ArgumentTypeDescriptor.CreateSimple(type), genericParametersCount: 0)),
        .. PrimitiveTypes.Select(type => CreateWrite(ArgumentTypeDescriptor.CreateSimple(type), genericParametersCount: 0)),
        CreateRead(ArgumentTypeDescriptor.CreateGenericMethodTypeParam(0), genericParametersCount: 1),
        CreateWrite(ArgumentTypeDescriptor.CreateGenericMethodTypeParam(0), genericParametersCount: 1),
    ];

    private static FieldAccessIntrinsicDescriptor CreateRead(ArgumentTypeDescriptor valueType, byte genericParametersCount)
    {
        return Create(
            methodName: "Read",
            signature: new MethodSignatureDescriptor(
                CallingConvention: GetCallingConvention(genericParametersCount),
                ParametersCount: 1,
                ReturnType: valueType,
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateByRef(valueType)],
                GenericParametersCount: genericParametersCount),
            interpretation: FieldAccessIntrinsicInterpretation.VolatileRead);
    }

    private static FieldAccessIntrinsicDescriptor CreateWrite(ArgumentTypeDescriptor valueType, byte genericParametersCount)
    {
        return Create(
            methodName: "Write",
            signature: new MethodSignatureDescriptor(
                CallingConvention: GetCallingConvention(genericParametersCount),
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateByRef(valueType), valueType],
                GenericParametersCount: genericParametersCount),
            interpretation: FieldAccessIntrinsicInterpretation.VolatileWrite);
    }

    private static CorCallingConvention GetCallingConvention(byte genericParametersCount)
        => genericParametersCount != 0
            ? CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT | CorCallingConvention.IMAGE_CEE_CS_CALLCONV_GENERIC
            : CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT;

    private static FieldAccessIntrinsicDescriptor Create(
        string methodName,
        MethodSignatureDescriptor signature,
        FieldAccessIntrinsicInterpretation interpretation)
    {
        return new FieldAccessIntrinsicDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: VolatileTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: signature,
            Interpretation: interpretation);
    }

    public static IEnumerable<FieldAccessIntrinsicDescriptor> GetAllIntrinsics() => AllIntrinsics;
}
