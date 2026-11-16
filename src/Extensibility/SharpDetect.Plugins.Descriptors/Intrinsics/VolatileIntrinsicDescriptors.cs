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
        .. PrimitiveTypes.Select(type => CreateRead(Sig.Simple(type), genericParametersCount: 0)),
        .. PrimitiveTypes.Select(type => CreateWrite(Sig.Simple(type), genericParametersCount: 0)),
        CreateRead(Sig.MethodTypeParameter(0), genericParametersCount: 1),
        CreateWrite(Sig.MethodTypeParameter(0), genericParametersCount: 1),
    ];

    private static FieldAccessIntrinsicDescriptor CreateRead(ArgumentTypeDescriptor valueType, byte genericParametersCount)
    {
        return Create(
            methodName: "Read",
            signature: MethodSignatureDescriptor.ForStaticMethod(
                returnType: valueType,
                parameterTypes: [Sig.ByRef(valueType)],
                genericParametersCount),
            interpretation: FieldAccessIntrinsicInterpretation.VolatileRead);
    }

    private static FieldAccessIntrinsicDescriptor CreateWrite(ArgumentTypeDescriptor valueType, byte genericParametersCount)
    {
        return Create(
            methodName: "Write",
            signature: MethodSignatureDescriptor.ForStaticMethod(
                returnType: Sig.Void,
                parameterTypes: [Sig.ByRef(valueType), valueType],
                genericParametersCount),
            interpretation: FieldAccessIntrinsicInterpretation.VolatileWrite);
    }

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
