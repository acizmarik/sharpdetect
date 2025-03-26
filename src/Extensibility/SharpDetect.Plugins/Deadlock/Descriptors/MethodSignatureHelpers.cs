// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Plugins.Descriptors;

public static class MethodSignatureHelpers
{
    private static readonly Dictionary<Type, CorElementType> corBasicTypesLookup = new()
    {
        { typeof(void), CorElementType.ELEMENT_TYPE_VOID },
        { typeof(object), CorElementType.ELEMENT_TYPE_OBJECT },
        { typeof(bool), CorElementType.ELEMENT_TYPE_BOOLEAN },
        { typeof(char), CorElementType.ELEMENT_TYPE_CHAR },
        { typeof(string), CorElementType.ELEMENT_TYPE_STRING },
        { typeof(nint), CorElementType.ELEMENT_TYPE_I },
        { typeof(nuint), CorElementType.ELEMENT_TYPE_U },
        { typeof(sbyte), CorElementType.ELEMENT_TYPE_I1 },
        { typeof(short), CorElementType.ELEMENT_TYPE_I2 },
        { typeof(int), CorElementType.ELEMENT_TYPE_I4 },
        { typeof(long), CorElementType.ELEMENT_TYPE_I8 },
        { typeof(byte), CorElementType.ELEMENT_TYPE_U1 },
        { typeof(ushort), CorElementType.ELEMENT_TYPE_U2 },
        { typeof(uint), CorElementType.ELEMENT_TYPE_U4 },
        { typeof(ulong), CorElementType.ELEMENT_TYPE_U8 },
        { typeof(float), CorElementType.ELEMENT_TYPE_R4 },
        { typeof(double), CorElementType.ELEMENT_TYPE_R8 },
    };

    public static CorElementType ConvertToCorType(this Type type)
    {
        if (type.IsEnum)
        {
            var underlyingType = type.GetEnumUnderlyingType()!.ConvertToCorType();
            return underlyingType;
        }
        else if (type.IsSZArray)
        {
            var underlyingType = type.GetElementType()!.ConvertToCorType();
            return CorElementType.ELEMENT_TYPE_SZARRAY | underlyingType;
        }
        else if (type.IsByRef)
        {
            var underlyingType = type.GetElementType()!.ConvertToCorType();
            return CorElementType.ELEMENT_TYPE_BYREF | underlyingType;
        }

        // FIXME: add support for more types mapping
        if (!corBasicTypesLookup.TryGetValue(type, out var corType))
            throw new NotSupportedException($"Type mapping from {type} to {nameof(CorElementType)} is not supported");
        return corType;
    }
}
