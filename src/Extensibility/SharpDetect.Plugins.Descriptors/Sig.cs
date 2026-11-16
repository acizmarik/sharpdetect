// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors;

public static class Sig
{
    public static ArgumentTypeDescriptor Void { get; } = Simple(CorElementType.ELEMENT_TYPE_VOID);
    public static ArgumentTypeDescriptor Boolean { get; } = Simple(CorElementType.ELEMENT_TYPE_BOOLEAN);
    public static ArgumentTypeDescriptor Int32 { get; } = Simple(CorElementType.ELEMENT_TYPE_I4);
    public static ArgumentTypeDescriptor Int64 { get; } = Simple(CorElementType.ELEMENT_TYPE_I8);
    public static ArgumentTypeDescriptor Object { get; } = Simple(CorElementType.ELEMENT_TYPE_OBJECT);

    public static ArgumentTypeDescriptor CancellationToken { get; } = ValueType("System.Threading.CancellationToken");
    public static ArgumentTypeDescriptor TimeSpan { get; } = ValueType("System.TimeSpan");

    public static ArgumentTypeDescriptor Simple(CorElementType elementType)
        => ArgumentTypeDescriptor.CreateSimple(elementType);

    public static ArgumentTypeDescriptor Class(string typeName)
        => ArgumentTypeDescriptor.CreateClass(typeName);

    public static ArgumentTypeDescriptor ValueType(string typeName)
        => ArgumentTypeDescriptor.CreateValueType(typeName);

    public static ArgumentTypeDescriptor ByRef(ArgumentTypeDescriptor innerType)
        => ArgumentTypeDescriptor.CreateByRef(innerType);

    public static ArgumentTypeDescriptor ArrayOf(ArgumentTypeDescriptor elementType)
        => ArgumentTypeDescriptor.CreateSZArray(elementType);

    public static ArgumentTypeDescriptor Generic(string genericTypeName, params ArgumentTypeDescriptor[] typeArguments)
        => ArgumentTypeDescriptor.CreateGenericInst(genericTypeName, typeArguments);

    public static ArgumentTypeDescriptor TypeParameter(int index)
        => ArgumentTypeDescriptor.CreateGenericTypeParam(index);

    public static ArgumentTypeDescriptor MethodTypeParameter(int index)
        => ArgumentTypeDescriptor.CreateGenericMethodTypeParam(index);
}
