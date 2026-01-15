// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Plugins.Descriptors;

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
            "Test_MonitorMethods_ExitIfLockTaken",
            "Test_ThreadMethods_Join1",
            "Test_ThreadMethods_Join2",
            "Test_ThreadMethods_Join3",
            "Test_MonitorMethods_Wait1",
            "Test_MonitorMethods_Wait2",
            "Test_MonitorMethods_Wait3_Reentrancy",
            "Test_ShadowCallstack_MonitorWait_ReentrancyWithPulse",
            "Test_ShadowCallstack_MonitorTryEnter_LockNotTaken",
            "Test_ShadowCallstack_MonitorPulse",
            "Test_ShadowCallstack_MonitorPulseAll",
            "Test_ThreadMethods_StartCallback1",
            "Test_ThreadMethods_StartCallback2",
            "Test_ThreadMethods_get_CurrentThread",
            "Test_LockMethods_EnterExit1",
            "Test_LockMethods_EnterExit2",
            "Test_LockMethods_TryEnterExit1",
            "Test_LockMethods_TryEnterExit2",
            "Test_SingleGarbageCollection_ObjectTracking_Simple",
            "Test_MultipleGarbageCollection_ObjectTracking_Simple",
            "Test_SingleGarbageCollection_ObjectTracking_MovedLockedObject",
            "Test_Field_ReferenceType_Static_Read",
            "Test_Field_ReferenceType_Static_Write",
            "Test_Field_ValueType_Static_Read",
            "Test_Field_ValueType_Static_Write",
        };

        foreach (var methodName in testMethodNames)
        {
            yield return new MethodDescriptor(
                MethodName: methodName,
                DeclaringTypeFullName: "SharpDetect.E2ETests.Subject.Program",
                VersionDescriptor: null,
                VoidMethodNoArgsSignature,
                InjectHooksRewritingDescriptor);
        }
    }
}