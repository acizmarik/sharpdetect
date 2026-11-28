// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class ThreadMethodDescriptors
{
    private const string ThreadTypeName = "System.Threading.Thread";

    private static readonly MethodDescriptor[] AllMethods =
    [
        // Common public API
        MethodDescriptor.ForInstanceMethod(ThreadTypeName, "Join")
            .Parameters(Sig.Int32)
            .Returns(Sig.Boolean)
            .WithManagedWrapper()
            .CaptureThis()
            .CaptureParameter(0, Capture.Int32)
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(RecordedEventType.ThreadJoinAttempt)
            .OnExit(RecordedEventType.ThreadJoinResult)
            .Build(),

        // Internal runtime API
        MethodDescriptor.ForInstanceMethod(ThreadTypeName, "StartCore")
            .CaptureThis()
            .OnEnter(RecordedEventType.ThreadStartCore)
            .Build(),
        MethodDescriptor.ForInstanceMethod(ThreadTypeName, "StartCallback")
            .OnRuntime(RuntimeVersions.Net8To10)
            .CaptureThis()
            .OnEnter(RecordedEventType.ThreadStartCallback)
            .Build(),
        MethodDescriptor.ForStaticMethod(ThreadTypeName, "StartCallback")
            .OnRuntime(RuntimeVersions.Net11)
            .Parameters(Sig.Pointer(Sig.Class(ThreadTypeName)))
            .CaptureParameter(0, Capture.DereferencedReference)
            .OnEnter(RecordedEventType.ThreadStartCallback)
            .Build(),
    ];

    public static IEnumerable<MethodDescriptor> GetAllMethods() => AllMethods;
}
