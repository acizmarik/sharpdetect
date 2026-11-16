// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors;

public record MethodSignatureDescriptor(
    CorCallingConvention CallingConvention,
    byte ParametersCount,
    ArgumentTypeDescriptor ReturnType,
    ArgumentTypeDescriptor[] ArgumentTypeElements,
    byte GenericParametersCount)
{
    public static MethodSignatureDescriptor ForStaticMethod(
        ArgumentTypeDescriptor returnType,
        ArgumentTypeDescriptor[] parameterTypes,
        byte genericParametersCount = 0)
        => Create(CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT, returnType, parameterTypes, genericParametersCount);

    public static MethodSignatureDescriptor ForInstanceMethod(
        ArgumentTypeDescriptor returnType,
        ArgumentTypeDescriptor[] parameterTypes,
        byte genericParametersCount = 0)
        => Create(CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS, returnType, parameterTypes, genericParametersCount);

    private static MethodSignatureDescriptor Create(
        CorCallingConvention callingConvention,
        ArgumentTypeDescriptor returnType,
        ArgumentTypeDescriptor[] parameterTypes,
        byte genericParametersCount)
    {
        if (genericParametersCount != 0)
            callingConvention |= CorCallingConvention.IMAGE_CEE_CS_CALLCONV_GENERIC;

        return new MethodSignatureDescriptor(
            callingConvention,
            checked((byte)parameterTypes.Length),
            returnType,
            parameterTypes,
            genericParametersCount);
    }
}
