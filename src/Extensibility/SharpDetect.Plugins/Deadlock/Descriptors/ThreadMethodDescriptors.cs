// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Descriptors;

namespace SharpDetect.Plugins.Deadlock.Descriptors;

internal static class ThreadMethodDescriptors
{
    private static readonly MethodDescriptor _threadJoinInt32;

    static ThreadMethodDescriptors()
    {
        const string typeFullName = "System.Threading.Thread";
        
        _threadJoinInt32 = new MethodDescriptor(
            MethodName: "Join",
            DeclaringTypeFullName: typeFullName,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: [ ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4) ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: true,
                Arguments: [
                    new(0, new((byte)nint.Size, CapturedValue.CaptureAsValue)),
                    new(1, new(sizeof(int), CapturedValue.CaptureAsValue))],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.ThreadJoinAttempt,
                MethodExitInterpretation: (ushort)RecordedEventType.ThreadJoinResult));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        // Common public API
        yield return _threadJoinInt32;
    }
}