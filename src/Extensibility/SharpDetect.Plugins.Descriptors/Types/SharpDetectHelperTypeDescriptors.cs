// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Types;

public static class SharpDetectHelperTypeDescriptors
{
    private static readonly TypeInjectionDescriptor CoreTypeDescriptor = new(
        TypeFullName: "SharpDetect",
        Methods:
        [
            .. CreateFieldAccessHelperPair("ReadStaticField", RecordedEventType.StaticFieldRead, isInstance: false),
            .. CreateFieldAccessHelperPair("ReadInstanceField", RecordedEventType.InstanceFieldRead, isInstance: true),
            .. CreateFieldAccessHelperPair("WriteStaticField", RecordedEventType.StaticFieldWrite, isInstance: false),
            .. CreateFieldAccessHelperPair("WriteInstanceField", RecordedEventType.InstanceFieldWrite, isInstance: true),
        ]);
    
    private static MethodInjectionDescriptor[] CreateFieldAccessHelperPair(
        string name,
        RecordedEventType eventType,
        bool isInstance)
    {
        var signature = FieldAccessHelperSignature.Create(isInstance);

        return
        [
            new MethodInjectionDescriptor(name, eventType, signature),
            new MethodInjectionDescriptor(name + "WithStack", eventType, signature, CaptureStackTrace: true),
        ];
    }

    public static IEnumerable<TypeInjectionDescriptor> GetAllTypes()
    {
        yield return CoreTypeDescriptor;
    }
}
