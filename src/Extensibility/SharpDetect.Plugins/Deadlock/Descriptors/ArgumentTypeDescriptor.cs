// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Deadlock.Descriptors;

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
}

