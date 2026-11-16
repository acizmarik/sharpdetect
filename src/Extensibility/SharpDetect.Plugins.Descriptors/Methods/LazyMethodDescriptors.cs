// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class LazyMethodDescriptors
{
    private const string LazyTypeName = "System.Lazy`1";

    private static readonly MethodDescriptor[] AllMethods =
    [
        MethodDescriptor.ForInstanceMethod(LazyTypeName, ".ctor")
            .Parameters(Sig.TypeParameter(0))
            .CaptureThis()
            .CaptureParameter(0, Capture.Reference)
            .OnEnter(RecordedEventType.ValuePublicationStore)
            .Build(),
        CreateLoad("CreateValue", RecordedEventType.ValuePublicationMaybeStoreLoad),
        CreateLoad("get_Value", RecordedEventType.ValuePublicationLoad),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;

    private static MethodDescriptor CreateLoad(string methodName, RecordedEventType exitInterpretation)
    {
        return MethodDescriptor.ForInstanceMethod(LazyTypeName, methodName)
            .Returns(Sig.TypeParameter(0))
            .CaptureThis()
            .CaptureReturnValue(Capture.Reference)
            .OnEnter(RecordedEventType.ValuePublicationContainerEnter)
            .OnExit(exitInterpretation)
            .Build();
    }
}
