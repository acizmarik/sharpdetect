// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class FieldAccessDescriptors
{
    private static readonly MethodDescriptor[] AllMethods =
    [
        .. CreateVariants("ReadStaticField", isInstance: false, RecordedEventType.StaticFieldRead),
        .. CreateVariants("WriteStaticField", isInstance: false, RecordedEventType.StaticFieldWrite),
        .. CreateVariants("ReadInstanceField", isInstance: true, RecordedEventType.InstanceFieldRead),
        .. CreateVariants("WriteInstanceField", isInstance: true, RecordedEventType.InstanceFieldWrite),
    ];

    private static MethodDescriptor[] CreateVariants(
        string methodName,
        bool isInstance,
        RecordedEventType enterInterpretation)
    {
        return
        [
            Create(methodName, isInstance, enterInterpretation, captureStackTraceOnEnter: false),
            Create(methodName + "WithStack", isInstance, enterInterpretation, captureStackTraceOnEnter: true),
        ];
    }

    private static MethodDescriptor Create(
        string methodName,
        bool isInstance,
        RecordedEventType enterInterpretation,
        bool captureStackTraceOnEnter)
    {
        CapturedArgumentDescriptor[] arguments = isInstance
            ?
            [
                new(0, new(sizeof(ulong), CapturedValue.CaptureAsValue)),
                new(1, new((byte)nint.Size, CapturedValue.CaptureAsReference)),
            ]
            :
            [
                new(0, new(sizeof(ulong), CapturedValue.CaptureAsValue))
            ];

        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: "SharpDetect",
            VersionDescriptor: null,
            SignatureDescriptor: FieldAccessHelperSignature.Create(isInstance),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: arguments,
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)enterInterpretation,
                MethodExitInterpretation: null,
                EmitExitEvent: false,
                CaptureStackTraceOnEnter: captureStackTraceOnEnter));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;
}
