// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class ConcurrentDictionaryMethodDescriptors
{
    private const string ConcurrentDictionaryTypeName = "System.Collections.Concurrent.ConcurrentDictionary`2";
    private const string FuncTypeName = "System.Func`2";
    private const string Func3TypeName = "System.Func`3";

    private static readonly CapturedArgumentDescriptor ContainerArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor ValueArg =
        new(2, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor OutValueArg =
        new(2, new((byte)nint.Size, CapturedValue.CaptureAsReference | CapturedValue.IndirectLoad));

    private static readonly CapturedValueDescriptor ReturnedValue =
        new((byte)nint.Size, CapturedValue.CaptureAsReference);

    private static ArgumentTypeDescriptor TKeyParam => ArgumentTypeDescriptor.CreateGenericTypeParam(0);
    private static ArgumentTypeDescriptor TValueParam => ArgumentTypeDescriptor.CreateGenericTypeParam(1);

    private static readonly MethodDescriptor TryAdd;
    private static readonly MethodDescriptor SetItem;
    private static readonly MethodDescriptor GetItem;
    private static readonly MethodDescriptor GetOrAddValue;
    private static readonly MethodDescriptor GetOrAddFactory;
    private static readonly MethodDescriptor TryGetValue;
    private static readonly MethodDescriptor TryRemove;
    private static readonly MethodDescriptor TryUpdate;
    private static readonly MethodDescriptor AddOrUpdateValue;
    private static readonly MethodDescriptor AddOrUpdateFactory;

    static ConcurrentDictionaryMethodDescriptors()
    {
        TryAdd = new MethodDescriptor(
            MethodName: "TryAdd",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: [TKeyParam, TValueParam]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg, ValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationStore,
                MethodExitInterpretation: null,
                EmitExitEvent: false));

        SetItem = new MethodDescriptor(
            MethodName: "set_Item",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [TKeyParam, TValueParam]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg, ValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationStore,
                MethodExitInterpretation: null,
                EmitExitEvent: false));

        GetItem = new MethodDescriptor(
            MethodName: "get_Item",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: TValueParam,
                ArgumentTypeElements: [TKeyParam]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg],
                ReturnValue: ReturnedValue,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationContainerEnter,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationLoad));

        GetOrAddValue = new MethodDescriptor(
            MethodName: "GetOrAdd",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: TValueParam,
                ArgumentTypeElements: [TKeyParam, TValueParam]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg],
                ReturnValue: ReturnedValue,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationContainerEnter,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationMaybeStoreLoad));

        GetOrAddFactory = new MethodDescriptor(
            MethodName: "GetOrAdd",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: TValueParam,
                ArgumentTypeElements:
                [
                    TKeyParam,
                    ArgumentTypeDescriptor.CreateGenericInst(FuncTypeName, TKeyParam, TValueParam)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg],
                ReturnValue: ReturnedValue,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationContainerEnter,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationMaybeStoreLoad));
        
        TryGetValue = new MethodDescriptor(
            MethodName: "TryGetValue",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: [TKeyParam, ArgumentTypeDescriptor.CreateByRef(TValueParam)]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg, OutValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationContainerEnter,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationLoadByRef));

        TryRemove = new MethodDescriptor(
            MethodName: "TryRemove",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: [TKeyParam, ArgumentTypeDescriptor.CreateByRef(TValueParam)]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg, OutValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationContainerEnter,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationLoadByRef));

        TryUpdate = new MethodDescriptor(
            MethodName: "TryUpdate",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 3,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: [TKeyParam, TValueParam, TValueParam]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg, ValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationStore,
                MethodExitInterpretation: null,
                EmitExitEvent: false));

        AddOrUpdateValue = new MethodDescriptor(
            MethodName: "AddOrUpdate",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 3,
                ReturnType: TValueParam,
                ArgumentTypeElements:
                [
                    TKeyParam,
                    TValueParam,
                    ArgumentTypeDescriptor.CreateGenericInst(Func3TypeName, TKeyParam, TValueParam, TValueParam)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg],
                ReturnValue: ReturnedValue,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationContainerEnter,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationStoreLoad));

        AddOrUpdateFactory = new MethodDescriptor(
            MethodName: "AddOrUpdate",
            DeclaringTypeFullName: ConcurrentDictionaryTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 3,
                ReturnType: TValueParam,
                ArgumentTypeElements:
                [
                    TKeyParam,
                    ArgumentTypeDescriptor.CreateGenericInst(FuncTypeName, TKeyParam, TValueParam),
                    ArgumentTypeDescriptor.CreateGenericInst(Func3TypeName, TKeyParam, TValueParam, TValueParam)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg],
                ReturnValue: ReturnedValue,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationContainerEnter,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationStoreLoad));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return TryAdd;
        yield return SetItem;
        yield return GetItem;
        yield return GetOrAddValue;
        yield return GetOrAddFactory;
        yield return TryGetValue;
        yield return TryRemove;
        yield return TryUpdate;
        yield return AddOrUpdateValue;
        yield return AddOrUpdateFactory;
    }
}
