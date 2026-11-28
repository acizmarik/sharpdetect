// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class ConcurrentCollectionMethodDescriptors
{
    private const string ConcurrentQueueTypeName = "System.Collections.Concurrent.ConcurrentQueue`1";
    private const string ConcurrentStackTypeName = "System.Collections.Concurrent.ConcurrentStack`1";
    private const string ConcurrentBagTypeName = "System.Collections.Concurrent.ConcurrentBag`1";

    private static ArgumentTypeDescriptor T => Sig.TypeParameter(0);

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return CreateStore(ConcurrentQueueTypeName, "Enqueue");
        yield return CreateLoad(ConcurrentQueueTypeName, "TryDequeue");
        yield return CreateLoad(ConcurrentQueueTypeName, "TryPeek");
        yield return CreateStore(ConcurrentStackTypeName, "Push", DeclaringAssemblies.CollectionsConcurrent);
        yield return CreateLoad(ConcurrentStackTypeName, "TryPop", DeclaringAssemblies.CollectionsConcurrent);
        yield return CreateLoad(ConcurrentStackTypeName, "TryPeek", DeclaringAssemblies.CollectionsConcurrent);
        yield return CreateStore(ConcurrentBagTypeName, "Add", DeclaringAssemblies.CollectionsConcurrent);
        yield return CreateLoad(ConcurrentBagTypeName, "TryTake", DeclaringAssemblies.CollectionsConcurrent);
        yield return CreateLoad(ConcurrentBagTypeName, "TryPeek", DeclaringAssemblies.CollectionsConcurrent);
    }

    private static MethodDescriptor CreateStore(
        string declaringTypeName,
        string methodName,
        string declaringAssemblyName = DeclaringAssemblies.CoreLib)
    {
        return MethodDescriptor.ForInstanceMethod(declaringTypeName, methodName)
            .InAssembly(declaringAssemblyName)
            .Parameters(T)
            .CaptureThis()
            .CaptureParameter(0, Capture.Reference)
            .OnEnter(RecordedEventType.ValuePublicationStore)
            .Build();
    }

    private static MethodDescriptor CreateLoad(
        string declaringTypeName,
        string methodName,
        string declaringAssemblyName = DeclaringAssemblies.CoreLib)
    {
        return MethodDescriptor.ForInstanceMethod(declaringTypeName, methodName)
            .InAssembly(declaringAssemblyName)
            .Parameters(Sig.ByRef(T))
            .Returns(Sig.Boolean)
            .CaptureThis()
            .CaptureParameter(0, Capture.OutReference)
            .OnEnter(RecordedEventType.ValuePublicationContainerEnter)
            .OnExit(RecordedEventType.ValuePublicationLoadByRef)
            .Build();
    }
}
