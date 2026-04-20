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
            "Test_TaskMethods_ScheduleAndStart1",
            "Test_TaskMethods_InnerInvoke1",
            "Test_TaskMethods_Wait1",
            "Test_TaskMethods_Wait2",
            "Test_TaskMethods_Wait3",
            "Test_TaskMethods_Wait4",
            "Test_TaskMethods_Wait5",
            "Test_TaskMethods_Result1",
            "Test_TaskMethods_Await1",
            "Test_TaskMethods_Await2",
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
            "Test_Field_ValueType_Instance_Read",
            "Test_Field_ValueType_Instance_Write",
            "Test_Field_ReferenceType_Instance_Read",
            "Test_Field_ReferenceType_Instance_Write",
            "Test_Field_ValueType_OnValueType_Instance_Read",
            "Test_Field_ValueType_OnValueType_Instance_Write",
            "Test_Field_ReferenceType_OnValueType_Instance_Read",
            "Test_Field_ReferenceType_OnValueType_Instance_Write",
            "Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Read",
            "Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Write",
            "Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Read",
            "Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Write",
            "Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Write",
            "Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Read",
            "Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Write",
            "Test_Field_Volatile_ValueType_Static_Read",
            "Test_Field_Volatile_ValueType_Static_Write",
            "Test_Field_Volatile_ValueType_Instance_Read",
            "Test_Field_Volatile_ValueType_Instance_Write",
            "Test_NoDataRace_SemaphoreSlim_ProtectedWriteRead",
            "Test_SemaphoreSlimMethods_WaitRelease1",
            "Test_SemaphoreSlimMethods_WaitRelease2",
            "Test_SemaphoreSlimMethods_WaitRelease3",
            "Test_SemaphoreSlimMethods_TryWaitRelease1",
            "Test_SemaphoreSlimMethods_TryWaitRelease2",
            "Test_SemaphoreSlimMethods_TryWaitRelease3",
            "Test_SemaphoreSlimMethods_TryWaitRelease4",
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