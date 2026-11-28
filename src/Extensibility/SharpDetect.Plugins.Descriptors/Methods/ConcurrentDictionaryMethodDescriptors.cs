// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class ConcurrentDictionaryMethodDescriptors
{
    private const string ConcurrentDictionaryTypeName = "System.Collections.Concurrent.ConcurrentDictionary`2";
    private const string FuncTypeName = "System.Func`2";
    private const string Func3TypeName = "System.Func`3";

    private static ArgumentTypeDescriptor TKey => Sig.TypeParameter(0);
    private static ArgumentTypeDescriptor TValue => Sig.TypeParameter(1);
    private static ArgumentTypeDescriptor ValueFactory => Sig.Generic(FuncTypeName, TKey, TValue);
    private static ArgumentTypeDescriptor UpdateFactory => Sig.Generic(Func3TypeName, TKey, TValue, TValue);

    private static readonly MethodDescriptor[] AllMethods =
    [
        CreateStore("TryAdd", Sig.Boolean, TKey, TValue),
        CreateStore("set_Item", Sig.Void, TKey, TValue),
        CreateLoad("get_Item", RecordedEventType.ValuePublicationLoad, TKey),
        CreateLoad("GetOrAdd", RecordedEventType.ValuePublicationMaybeStoreLoad, TKey, TValue),
        CreateLoad("GetOrAdd", RecordedEventType.ValuePublicationMaybeStoreLoad, TKey, ValueFactory),
        CreateLoadByRef("TryGetValue"),
        CreateLoadByRef("TryRemove"),
        CreateStore("TryUpdate", Sig.Boolean, TKey, TValue, TValue),
        CreateLoad("AddOrUpdate", RecordedEventType.ValuePublicationStoreLoad, TKey, TValue, UpdateFactory),
        CreateLoad("AddOrUpdate", RecordedEventType.ValuePublicationStoreLoad, TKey, ValueFactory, UpdateFactory),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    // (TKey key, TValue value, ...): the stored value is the second parameter.
    private static MethodDescriptor CreateStore(
        string methodName,
        ArgumentTypeDescriptor returnType,
        params ArgumentTypeDescriptor[] parameterTypes)
    {
        return ConcurrentDictionary(methodName)
            .Parameters(parameterTypes)
            .Returns(returnType)
            .CaptureParameter(1, Capture.Reference)
            .OnEnter(RecordedEventType.ValuePublicationStore)
            .Build();
    }

    // The loaded value is the return value.
    private static MethodDescriptor CreateLoad(
        string methodName,
        RecordedEventType exitInterpretation,
        params ArgumentTypeDescriptor[] parameterTypes)
    {
        return ConcurrentDictionary(methodName)
            .Parameters(parameterTypes)
            .Returns(TValue)
            .CaptureReturnValue(Capture.Reference)
            .OnEnter(RecordedEventType.ValuePublicationContainerEnter)
            .OnExit(exitInterpretation)
            .Build();
    }

    private static MethodDescriptor CreateLoadByRef(string methodName)
    {
        return ConcurrentDictionary(methodName)
            .Parameters(TKey, Sig.ByRef(TValue))
            .Returns(Sig.Boolean)
            .CaptureParameter(1, Capture.OutReference)
            .OnEnter(RecordedEventType.ValuePublicationContainerEnter)
            .OnExit(RecordedEventType.ValuePublicationLoadByRef)
            .Build();
    }

    private static MethodDescriptorBuilder ConcurrentDictionary(string methodName)
    {
        return MethodDescriptor.ForInstanceMethod(ConcurrentDictionaryTypeName, methodName)
            .InAssembly(DeclaringAssemblies.CollectionsConcurrent)
            .CaptureThis();
    }
}
