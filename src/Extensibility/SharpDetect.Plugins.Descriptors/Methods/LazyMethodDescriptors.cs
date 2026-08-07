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

    private static readonly MethodDescriptor GetValue;

    static LazyMethodDescriptors()
    {
        GetValue = new MethodDescriptor(
            MethodName: "get_Value",
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
                MethodExitInterpretation: (ushort)RecordedEventType.ValuePublicationMaybeStoreLoad));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return GetValue;
    }
}
