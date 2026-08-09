// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class LazyMethodDescriptors
{
    private const string LazyTypeName = "System.Lazy`1";

    private static readonly CapturedArgumentDescriptor ContainerArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor ValueArg =
        new(1, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly MethodDescriptor GetValue;
    private static readonly MethodDescriptor CreateValue;
    private static readonly MethodDescriptor ValueConstructor;

    static LazyMethodDescriptors()
    {
        CreateValue = CreateDescriptor("CreateValue", RecordedEventType.ValuePublicationMaybeStoreLoad);
        GetValue = CreateDescriptor("get_Value", RecordedEventType.ValuePublicationLoad);
        ValueConstructor = new MethodDescriptor(
            MethodName: ".ctor",
            DeclaringTypeFullName: LazyTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateGenericTypeParam(0)]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg, ValueArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationStore,
                MethodExitInterpretation: null,
                EmitExitEvent: false));
    }

    private static MethodDescriptor CreateDescriptor(string methodName, RecordedEventType exitInterpretation)
    {
        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: LazyTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateGenericTypeParam(0),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ContainerArg],
                ReturnValue: new CapturedValueDescriptor((byte)nint.Size, CapturedValue.CaptureAsReference),
                MethodEnterInterpretation: (ushort)RecordedEventType.ValuePublicationContainerEnter,
                MethodExitInterpretation: (ushort)exitInterpretation));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return ValueConstructor;
        yield return CreateValue;
        yield return GetValue;
    }
}
