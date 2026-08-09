// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Intrinsics;

public static class InterlockedIntrinsicDescriptors
{
    private const string InterlockedTypeName = "System.Threading.Interlocked";
    
    private static readonly CorElementType[] IntegralTypes =
    [
        CorElementType.ELEMENT_TYPE_I4,
        CorElementType.ELEMENT_TYPE_U4,
        CorElementType.ELEMENT_TYPE_I8,
        CorElementType.ELEMENT_TYPE_U8
    ];
    
    private static readonly CorElementType[] ExchangeTypes =
    [
        .. IntegralTypes,
        CorElementType.ELEMENT_TYPE_R4,
        CorElementType.ELEMENT_TYPE_R8,
        CorElementType.ELEMENT_TYPE_I,
        CorElementType.ELEMENT_TYPE_U,
        CorElementType.ELEMENT_TYPE_OBJECT
    ];

    private static readonly FieldAccessIntrinsicDescriptor[] AllIntrinsics =
    [
        .. CreateOverloads("Increment", IntegralTypes, additionalParametersCount: 0),
        .. CreateOverloads("Decrement", IntegralTypes, additionalParametersCount: 0),
        .. CreateOverloads(
            "Read",
            [CorElementType.ELEMENT_TYPE_I8, CorElementType.ELEMENT_TYPE_U8],
            additionalParametersCount: 0,
            FieldAccessIntrinsicInterpretation.VolatileRead),
        .. CreateOverloads("Add", IntegralTypes, additionalParametersCount: 1),
        .. CreateOverloads("And", IntegralTypes, additionalParametersCount: 1),
        .. CreateOverloads("Or", IntegralTypes, additionalParametersCount: 1),
        .. CreateOverloads("Exchange", ExchangeTypes, additionalParametersCount: 1),
        .. CreateOverloads("CompareExchange", ExchangeTypes, additionalParametersCount: 2),
        CreateGenericOverload("Exchange", additionalParametersCount: 1),
        CreateGenericOverload("CompareExchange", additionalParametersCount: 2),
    ];

    private static FieldAccessIntrinsicDescriptor[] CreateOverloads(
        string methodName,
        CorElementType[] elementTypes,
        byte additionalParametersCount,
        FieldAccessIntrinsicInterpretation interpretation = FieldAccessIntrinsicInterpretation.AtomicReadModifyWrite)
    {
        return
        [
            .. elementTypes.Select(type => Create(
                methodName,
                ArgumentTypeDescriptor.CreateSimple(type),
                additionalParametersCount,
                genericParametersCount: 0,
                interpretation))
        ];
    }

    private static FieldAccessIntrinsicDescriptor CreateGenericOverload(string methodName, byte additionalParametersCount)
    {
        return Create(
            methodName,
            ArgumentTypeDescriptor.CreateGenericMethodTypeParam(0),
            additionalParametersCount,
            genericParametersCount: 1,
            FieldAccessIntrinsicInterpretation.AtomicReadModifyWrite);
    }

    private static FieldAccessIntrinsicDescriptor Create(
        string methodName,
        ArgumentTypeDescriptor valueType,
        byte additionalParametersCount,
        byte genericParametersCount,
        FieldAccessIntrinsicInterpretation interpretation)
    {
        var callingConvention = genericParametersCount != 0
            ? CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT | CorCallingConvention.IMAGE_CEE_CS_CALLCONV_GENERIC
            : CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT;

        return new FieldAccessIntrinsicDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: InterlockedTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: callingConvention,
                ParametersCount: (byte)(additionalParametersCount + 1),
                ReturnType: valueType,
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateByRef(valueType),
                    .. Enumerable.Repeat(valueType, additionalParametersCount)
                ],
                GenericParametersCount: genericParametersCount),
            Interpretation: interpretation);
    }

    public static IEnumerable<FieldAccessIntrinsicDescriptor> GetAllIntrinsics() => AllIntrinsics;
}
