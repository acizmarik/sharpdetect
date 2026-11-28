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
        .. CreateAll(DeclaringAssemblies.CollectionsConcurrent, RuntimeVersions.Net8To10),
        .. CreateAll(DeclaringAssemblies.CoreLib, RuntimeVersions.Net11),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static IEnumerable<MethodDescriptor> CreateAll(string declaringAssemblyName, MethodVersionDescriptor version)
    {
        MethodDescriptorBuilder ConcurrentDictionary(string methodName)
        {
            return MethodDescriptor.ForInstanceMethod(ConcurrentDictionaryTypeName, methodName)
                .InAssembly(declaringAssemblyName)
                .OnRuntime(version)
                .CaptureThis();
        }

        yield return CreateStore(ConcurrentDictionary("TryAdd"), Sig.Boolean, TKey, TValue);
        yield return CreateStore(ConcurrentDictionary("set_Item"), Sig.Void, TKey, TValue);
        yield return CreateLoad(ConcurrentDictionary("get_Item"), RecordedEventType.ValuePublicationLoad, TKey);
        yield return CreateLoad(ConcurrentDictionary("GetOrAdd"), RecordedEventType.ValuePublicationMaybeStoreLoad, TKey, TValue);
        yield return CreateLoad(ConcurrentDictionary("GetOrAdd"), RecordedEventType.ValuePublicationMaybeStoreLoad, TKey, ValueFactory);
        yield return CreateLoadByRef(ConcurrentDictionary("TryGetValue"));
        yield return CreateLoadByRef(ConcurrentDictionary("TryRemove"));
        yield return CreateStore(ConcurrentDictionary("TryUpdate"), Sig.Boolean, TKey, TValue, TValue);
        yield return CreateLoad(ConcurrentDictionary("AddOrUpdate"), RecordedEventType.ValuePublicationStoreLoad, TKey, TValue, UpdateFactory);
        yield return CreateLoad(ConcurrentDictionary("AddOrUpdate"), RecordedEventType.ValuePublicationStoreLoad, TKey, ValueFactory, UpdateFactory);
    }

    // (TKey key, TValue value, ...): the stored value is the second parameter.
    private static MethodDescriptor CreateStore(
        MethodDescriptorBuilder method,
        ArgumentTypeDescriptor returnType,
        params ArgumentTypeDescriptor[] parameterTypes)
    {
        return method
            .Parameters(parameterTypes)
            .Returns(returnType)
            .CaptureParameter(1, Capture.Reference)
            .OnEnter(RecordedEventType.ValuePublicationStore)
            .Build();
    }

    // The loaded value is the return value.
    private static MethodDescriptor CreateLoad(
        MethodDescriptorBuilder method,
        RecordedEventType exitInterpretation,
        params ArgumentTypeDescriptor[] parameterTypes)
    {
        return method
            .Parameters(parameterTypes)
            .Returns(TValue)
            .CaptureReturnValue(Capture.Reference)
            .OnEnter(RecordedEventType.ValuePublicationContainerEnter)
            .OnExit(exitInterpretation)
            .Build();
    }

    private static MethodDescriptor CreateLoadByRef(MethodDescriptorBuilder method)
    {
        return method
            .Parameters(TKey, Sig.ByRef(TValue))
            .Returns(Sig.Boolean)
            .CaptureParameter(1, Capture.OutReference)
            .OnEnter(RecordedEventType.ValuePublicationContainerEnter)
            .OnExit(RecordedEventType.ValuePublicationLoadByRef)
            .Build();
    }
}
