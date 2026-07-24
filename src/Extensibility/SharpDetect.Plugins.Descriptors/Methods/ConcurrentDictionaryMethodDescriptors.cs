// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class ConcurrentDictionaryMethodDescriptors
{
    private const string ConcurrentDictionaryTypeName = "System.Collections.Concurrent.ConcurrentDictionary`2";
    private const string FuncTypeName = "System.Func`2";

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
                Arguments: [ValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationStore,
                MethodExitInterpretation: null));

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
                Arguments: [ValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationStore,
                MethodExitInterpretation: null));

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
                Arguments: [],
                ReturnValue: ReturnedValue,
                MethodEnterInterpretation: null,
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
                Arguments: [],
                ReturnValue: ReturnedValue,
                MethodEnterInterpretation: null,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationStoreLoad));

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
                Arguments: [],
                ReturnValue: ReturnedValue,
                MethodEnterInterpretation: null,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationStoreLoad));
        
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
                Arguments: [OutValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: null,
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationLoadByRef));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return TryAdd;
        yield return SetItem;
        yield return GetItem;
        yield return GetOrAddValue;
        yield return GetOrAddFactory;
        yield return TryGetValue;
    }
}
