// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Descriptors;
using SharpDetect.Plugins.Deadlock.Descriptors;

namespace SharpDetect.E2ETests.Utils;

internal static class TestMethodDescriptors
{
    private static readonly MethodSignatureDescriptor VoidMethodNoArgsSignature = new (
        CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
        ParametersCount: 0,
        ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
        ArgumentTypeElements: []);

    private static readonly MethodRewritingDescriptor InjectHooksRewritingDescriptor = new(
        InjectHooks: true,
        InjectManagedWrapper: false,
        Arguments: [],
        ReturnValue: null,
        MethodEnterInterpretation: null,
        MethodExitInterpretation: null);
    
    public static IEnumerable<MethodDescriptor> GetAllTestMethods()
    {
        // Return descriptors for all test methods that need hooks
        var testMethodNames = new[]
        {
            "Test_MonitorMethods_EnterExit1",
            "Test_MonitorMethods_EnterExit2",
            "Test_MonitorMethods_TryEnterExit1",
            "Test_MonitorMethods_TryEnterExit2",
            "Test_MonitorMethods_TryEnterExit3",
            "Test_ThreadMethods_Join1",
            "Test_ThreadMethods_Join2",
            "Test_ThreadMethods_Join3",
            "Test_SingleGarbageCollection_ObjectTracking_Simple",
            "Test_MultipleGarbageCollection_ObjectTracking_Simple",
            "Test_SingleGarbageCollection_ObjectTracking_MovedLockedObject"
        };

        foreach (var methodName in testMethodNames)
        {
            yield return new MethodDescriptor(
                MethodName: methodName,
                DeclaringTypeFullName: "SharpDetect.E2ETests.Subject.Program",
                VoidMethodNoArgsSignature,
                InjectHooksRewritingDescriptor);
        }
    }
}