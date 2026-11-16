// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class ConcurrentCollectionMethodDescriptors
{
    private const string ConcurrentQueueTypeName = "System.Collections.Concurrent.ConcurrentQueue`1";
    private const string ConcurrentStackTypeName = "System.Collections.Concurrent.ConcurrentStack`1";
    private const string ConcurrentBagTypeName = "System.Collections.Concurrent.ConcurrentBag`1";

    private static readonly CapturedArgumentDescriptor ContainerArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor ValueArg =
        new(1, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor OutValueArg =
        new(1, new((byte)nint.Size, CapturedValue.CaptureAsReference | CapturedValue.IndirectLoad));

    private static ArgumentTypeDescriptor TParam => ArgumentTypeDescriptor.CreateGenericTypeParam(0);

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
        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: declaringTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [TParam]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg, ValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationStore,
                MethodExitInterpretation: null,
                EmitExitEvent: false),
            DeclaringAssemblyName: declaringAssemblyName);
    }

    private static MethodDescriptor CreateLoad(
        string declaringTypeName,
        string methodName,
        string declaringAssemblyName = DeclaringAssemblies.CoreLib)
    {
        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: declaringTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateByRef(TParam)]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg, OutValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationContainerEnter,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationLoadByRef),
            DeclaringAssemblyName: declaringAssemblyName);
    }
}
