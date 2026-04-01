// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors;

public record ArgumentTypeDescriptor
{
    public CorElementType[] ElementTypes { get; init; }
    public string? TypeName { get; init; }

    private ArgumentTypeDescriptor(
        CorElementType[] elementTypes,
        string? typeName = null)
    {
        ElementTypes = elementTypes;
        TypeName = typeName;
    }
    
    public static ArgumentTypeDescriptor CreateSimple(CorElementType elementType) 
        => new([elementType]);
    
    public static ArgumentTypeDescriptor CreateClass(string typeName) 
        => new([CorElementType.ELEMENT_TYPE_CLASS], typeName);
    
    public static ArgumentTypeDescriptor CreateValueType(string typeName) 
        => new([CorElementType.ELEMENT_TYPE_VALUETYPE], typeName);
    
    public static ArgumentTypeDescriptor CreateByRef(ArgumentTypeDescriptor innerType) 
        => new([CorElementType.ELEMENT_TYPE_BYREF, .. innerType.ElementTypes], innerType.TypeName);
    
    public static ArgumentTypeDescriptor CreateSZArray(ArgumentTypeDescriptor elementType) 
        => new([CorElementType.ELEMENT_TYPE_SZARRAY, .. elementType.ElementTypes], elementType.TypeName);
    
    public static ArgumentTypeDescriptor CreateGenericTypeParam(int index) 
        => CreateGenericParam(CorElementType.ELEMENT_TYPE_VAR, index);
    
    public static ArgumentTypeDescriptor CreateGenericMethodTypeParam(int index) 
        => CreateGenericParam(CorElementType.ELEMENT_TYPE_MVAR, index);
    
    private static ArgumentTypeDescriptor CreateGenericParam(CorElementType type, int index)
    {
        return (uint)index < 0x80
            ? new ArgumentTypeDescriptor([type, (CorElementType)index])
            : throw new ArgumentOutOfRangeException(nameof(index), "Generic parameter index >= 128 is not supported.");
    }
}

