// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Descriptors;

namespace SharpDetect.E2ETests.Utils;

internal static class TestMethodDescriptors
{
    private static readonly MethodSignatureDescriptor VoidMethodWithStringArraySignature = new (
        CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
        ParametersCount: 1,
        ReturnType: CorElementType.ELEMENT_TYPE_VOID,
        ArgumentTypeElements: [CorElementType.ELEMENT_TYPE_SZARRAY, CorElementType.ELEMENT_TYPE_STRING]);

    private static readonly MethodRewritingDescriptor InjectHooksRewritingDescriptor = new(
        InjectHooks: true,
        InjectManagedWrapper: false,
        Arguments: [],
        ReturnValue: null,
        MethodEnterInterpretation: null,
        MethodExitInterpretation: null);
    
    public static MethodDescriptor BuildTestEntryMethod()
    {
        return new MethodDescriptor(
            MethodName: "Main",
            DeclaringTypeFullName: "SharpDetect.E2ETests.Subject.Program",
            VoidMethodWithStringArraySignature,
            InjectHooksRewritingDescriptor);
    }
}