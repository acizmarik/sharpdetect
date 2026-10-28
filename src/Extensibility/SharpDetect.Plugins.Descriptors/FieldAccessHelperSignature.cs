// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors;

public static class FieldAccessHelperSignature
{
    private static ArgumentTypeDescriptor[] CreateArgumentTypes(bool isInstance, bool isGenericStatic) => (isInstance, isGenericStatic) switch
    {
        (true, _) =>
        [
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I8),
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)
        ],
        (false, true) =>
        [
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I8),
            ArgumentTypeDescriptor.CreateValueType("System.RuntimeTypeHandle")
        ],
        (false, false) =>
        [
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I8)
        ]
    };

    public static MethodSignatureDescriptor Create(bool isInstance, bool isGenericStatic = false)
    {
        var argumentTypes = CreateArgumentTypes(isInstance, isGenericStatic);
        return new MethodSignatureDescriptor(
            CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
            ParametersCount: (byte)argumentTypes.Length,
            ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
            ArgumentTypeElements: argumentTypes);
    }
}
