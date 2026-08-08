// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

// FIXME: generic method overloads on Interlocked are not yet supported
public static class InterlockedMethodDescriptors
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

    private static readonly MethodDescriptor[] AllMethods =
    [
        .. CreateUnary("Increment", IntegralTypes),
        .. CreateUnary("Decrement", IntegralTypes),
        .. CreateUnary("Read", [CorElementType.ELEMENT_TYPE_I8, CorElementType.ELEMENT_TYPE_U8]),
        .. CreateBinary("Add", IntegralTypes),
        .. CreateBinary("And", IntegralTypes),
        .. CreateBinary("Or", IntegralTypes),
        .. CreateBinary("Exchange", ExchangeTypes),
        .. CreateTernary("CompareExchange", ExchangeTypes),
    ];

    private static MethodDescriptor[] CreateUnary(string methodName, CorElementType[] elementTypes)
        => [.. elementTypes.Select(type => Create(methodName, type, additionalParametersCount: 0))];

    private static MethodDescriptor[] CreateBinary(string methodName, CorElementType[] elementTypes)
        => [.. elementTypes.Select(type => Create(methodName, type, additionalParametersCount: 1))];

    private static MethodDescriptor[] CreateTernary(string methodName, CorElementType[] elementTypes)
        => [.. elementTypes.Select(type => Create(methodName, type, additionalParametersCount: 2))];

    private static MethodDescriptor Create(
        string methodName,
        CorElementType elementType,
        byte additionalParametersCount)
    {
        var valueType = ArgumentTypeDescriptor.CreateSimple(elementType);
        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: InterlockedTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: (byte)(additionalParametersCount + 1),
                ReturnType: valueType,
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateByRef(valueType),
                    .. Enumerable.Repeat(valueType, additionalParametersCount)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: false,
                InjectManagedWrapper: false,
                Arguments: [],
                ReturnValue: null,
                MethodEnterInterpretation: null,
                MethodExitInterpretation: null,
                FieldAddressAccessInterpretation: FieldAddressAccessInterpretation.AtomicReadModifyWrite));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;
}
