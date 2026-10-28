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
        .. CreateVariants("ReadGenericStaticField", isInstance: false, RecordedEventType.GenericStaticFieldRead, isGenericStatic: true),
        .. CreateVariants("WriteGenericStaticField", isInstance: false, RecordedEventType.GenericStaticFieldWrite, isGenericStatic: true),
        .. CreateVariants("ReadInstanceField", isInstance: true, RecordedEventType.InstanceFieldRead),
        .. CreateVariants("WriteInstanceField", isInstance: true, RecordedEventType.InstanceFieldWrite),
    ];

    private static MethodDescriptor[] CreateVariants(
        string methodName,
        bool isInstance,
        RecordedEventType enterInterpretation,
        bool isGenericStatic = false)
    {
        return
        [
            Create(methodName, isInstance, enterInterpretation, isGenericStatic),
            Create(methodName + "WithStack", isInstance, enterInterpretation, isGenericStatic),
        ];
    }

    private static MethodDescriptor Create(
        string methodName,
        bool isInstance,
        RecordedEventType enterInterpretation,
        bool isGenericStatic)
    {
        CapturedArgumentDescriptor[] arguments = (isInstance, isGenericStatic) switch
        {
            (true, _) =>
            [
                new(0, new(sizeof(ulong), CapturedValue.CaptureAsValue)),
                new(1, new((byte)nint.Size, CapturedValue.CaptureAsReference)),
            ],
            (false, true) =>
            [
                new(0, new(sizeof(ulong), CapturedValue.CaptureAsValue)),
                new(1, new((byte)nint.Size, CapturedValue.CaptureAsValue)),
            ],
            (false, false) =>
            [
                new(0, new(sizeof(ulong), CapturedValue.CaptureAsValue))
            ]
        };

        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: "SharpDetect",
            VersionDescriptor: null,
            SignatureDescriptor: FieldAccessHelperSignature.Create(isInstance, isGenericStatic),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: false,
                InjectManagedWrapper: false,
                Arguments: arguments,
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)enterInterpretation,
                MethodExitInterpretation: null,
                EmitExitEvent: false));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;
}
