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
        var builder = MethodDescriptor.ForStaticMethod("SharpDetect", methodName)
            .WithoutHooks()
            .Parameters(FieldAccessHelperSignature.GetParameterTypes(isInstance, isGenericStatic))
            .CaptureParameter(0, Capture.Int64)
            .OnEnter(enterInterpretation);

        if (isInstance)
            builder.CaptureParameter(1, Capture.Reference);
        else if (isGenericStatic)
            builder.CaptureParameter(1, Capture.NativeInt);

        return builder.Build();
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;
}
