// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject
{
    public static partial class Program
    {
        public static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "TEST_MainThread";
            Console.WriteLine("Args: " + args[0]);

            // Shadow runtime integrity tests
            switch (args[0])
            {
                case nameof(Test_ShadowCallstack_MonitorWait_ReentrancyWithPulse):
                    Test_ShadowCallstack_MonitorWait_ReentrancyWithPulse();
                    break;
                case nameof(Test_ShadowCallstack_MonitorTryEnter_LockNotTaken):
                    Test_ShadowCallstack_MonitorTryEnter_LockNotTaken();
                    break;
                case nameof(Test_ShadowCallstack_MonitorPulse):
                    Test_ShadowCallstack_MonitorPulse();
                    break;
                case nameof(Test_ShadowCallstack_MonitorPulseAll):
                    Test_ShadowCallstack_MonitorPulseAll();
                    break;
                case nameof(Test_ShadowCallstack_SyncMethodThrowsInsideTaskBody):
                    Test_ShadowCallstack_SyncMethodThrowsInsideTaskBody();
                    break;
                case nameof(Test_ShadowCallstack_FaultedTaskJoinThrows):
                    Test_ShadowCallstack_FaultedTaskJoinThrows();
                    break;
#if NET10_0_OR_GREATER
                case nameof(Test_ShadowCallstack_MonitorExitIfLockTaken):
                    Test_ShadowCallstack_MonitorExitIfLockTaken();
                    break;
#endif
            }
            
            // Method interpretation events
            switch (args[0])
            {
                case nameof(Test_MonitorMethods_EnterExit1):
                    Test_MonitorMethods_EnterExit1();
                    break;
                case nameof(Test_MonitorMethods_EnterExit2):
                    Test_MonitorMethods_EnterExit2();
                    break;
                case nameof(Test_MonitorMethods_TryEnterExit1):
                    Test_MonitorMethods_TryEnterExit1();
                    break;
                case nameof(Test_MonitorMethods_TryEnterExit2):
                    Test_MonitorMethods_TryEnterExit2();
                    break;
                case nameof(Test_MonitorMethods_TryEnterExit3):
                    Test_MonitorMethods_TryEnterExit3();
                    break;
                case nameof(Test_MonitorMethods_EnterExitLoop):
                    Test_MonitorMethods_EnterExitLoop();
                    break;
                case nameof(Test_MonitorMethods_TryEnterExitLoop):
                    Test_MonitorMethods_TryEnterExitLoop();
                    break;
                case nameof(Test_MonitorMethods_Wait1):
                    Test_MonitorMethods_Wait1();
                    break;
                case nameof(Test_MonitorMethods_Wait2):
                    Test_MonitorMethods_Wait2();
                    break;
                case nameof(Test_MonitorMethods_Wait3_Reentrancy):
                    Test_MonitorMethods_Wait3_Reentrancy();
                    break;
#if NET10_0_OR_GREATER
                case nameof(Test_MonitorMethods_ExitIfLockTaken):
                    Test_MonitorMethods_ExitIfLockTaken();
                    break;
#endif
                case nameof(Test_ThreadMethods_Join1):
                    Test_ThreadMethods_Join1();
                    break;
                case nameof(Test_ThreadMethods_Join2):
                    Test_ThreadMethods_Join2();
                    break;
                case nameof(Test_ThreadMethods_Join3):
                    Test_ThreadMethods_Join3();
                    break;
                case nameof(Test_ThreadMethods_StartCallback1):
                    Test_ThreadMethods_StartCallback1();
                    break;
                case nameof(Test_ThreadMethods_get_CurrentThread):
                    Test_ThreadMethods_get_CurrentThread();
                    break;
                case nameof(Test_TaskMethods_ScheduleAndStart1):
                    Test_TaskMethods_ScheduleAndStart1();
                    break;
                case nameof(Test_TaskMethods_InnerInvoke1):
                    Test_TaskMethods_InnerInvoke1();
                    break;
                case nameof(Test_TaskMethods_Wait1):
                    Test_TaskMethods_Wait1();
                    break;
                case nameof(Test_TaskMethods_Wait2):
                    Test_TaskMethods_Wait2();
                    break;
                case nameof(Test_TaskMethods_Wait3):
                    Test_TaskMethods_Wait3();
                    break;
                case nameof(Test_TaskMethods_Wait4):
                    Test_TaskMethods_Wait4();
                    break;
                case nameof(Test_TaskMethods_Wait5):
                    Test_TaskMethods_Wait5();
                    break;
                case nameof(Test_TaskMethods_Result1):
                    Test_TaskMethods_Result1();
                    break;
                case nameof(Test_TaskMethods_Await1):
                    Test_TaskMethods_Await1();
                    break;
                case nameof(Test_TaskMethods_Await2):
                    Test_TaskMethods_Await2();
                    break;
#if NET9_0_OR_GREATER
                case nameof(Test_LockMethods_EnterExit1):
                    Test_LockMethods_EnterExit1();
                    break;
                case nameof(Test_LockMethods_EnterExit2):
                    Test_LockMethods_EnterExit2();
                    break;
                case nameof(Test_LockMethods_TryEnterExit1):
                    Test_LockMethods_TryEnterExit1();
                    break;
                case nameof(Test_LockMethods_TryEnterExit2):
                    Test_LockMethods_TryEnterExit2();
                    break;
#endif
                case nameof(Test_SemaphoreSlimMethods_WaitRelease1):
                    Test_SemaphoreSlimMethods_WaitRelease1();
                    break;
                case nameof(Test_SemaphoreSlimMethods_WaitRelease2):
                    Test_SemaphoreSlimMethods_WaitRelease2();
                    break;
                case nameof(Test_SemaphoreSlimMethods_WaitRelease3):
                    Test_SemaphoreSlimMethods_WaitRelease3();
                    break;
                case nameof(Test_SemaphoreSlimMethods_TryWaitRelease1):
                    Test_SemaphoreSlimMethods_TryWaitRelease1();
                    break;
                case nameof(Test_SemaphoreSlimMethods_TryWaitRelease2):
                    Test_SemaphoreSlimMethods_TryWaitRelease2();
                    break;
                case nameof(Test_SemaphoreSlimMethods_TryWaitRelease3):
                    Test_SemaphoreSlimMethods_TryWaitRelease3();
                    break;
                case nameof(Test_SemaphoreSlimMethods_TryWaitRelease4):
                    Test_SemaphoreSlimMethods_TryWaitRelease4();
                    break;
                case nameof(Test_MutexMethods_WaitOneRelease1):
                    Test_MutexMethods_WaitOneRelease1();
                    break;
                case nameof(Test_MutexMethods_WaitOneRelease2):
                    Test_MutexMethods_WaitOneRelease2();
                    break;
                case nameof(Test_SemaphoreMethods_WaitOneRelease1):
                    Test_SemaphoreMethods_WaitOneRelease1();
                    break;
                case nameof(Test_SemaphoreMethods_WaitOneRelease2):
                    Test_SemaphoreMethods_WaitOneRelease2();
                    break;
                case nameof(Test_EventWaitHandleMethods_AutoReset_SetWaitOne):
                    Test_EventWaitHandleMethods_AutoReset_SetWaitOne();
                    break;
                case nameof(Test_EventWaitHandleMethods_ManualReset_SetWaitOne):
                    Test_EventWaitHandleMethods_ManualReset_SetWaitOne();
                    break;
                case nameof(Test_SignalAndWaitMethods_Events):
                    Test_SignalAndWaitMethods_Events();
                    break;
                case nameof(Test_SignalAndWaitMethods_MutexSignal):
                    Test_SignalAndWaitMethods_MutexSignal();
                    break;
                case nameof(Test_AbandonedMutexExceptionMethods_Construct):
                    Test_AbandonedMutexExceptionMethods_Construct();
                    break;
                case nameof(Test_WaitMultipleMethods_WaitAll):
                    Test_WaitMultipleMethods_WaitAll();
                    break;
                case nameof(Test_WaitMultipleMethods_WaitAny):
                    Test_WaitMultipleMethods_WaitAny();
                    break;
                case nameof(Test_EventWaitHandleMethods_ManualReset_SetResetSet):
                    Test_EventWaitHandleMethods_ManualReset_SetResetSet();
                    break;
                case nameof(Test_LazyMethods_GetValue):
                    Test_LazyMethods_GetValue();
                    break;
            }

            // Field events
            switch (args[0])
            {
                // Regular fields
                case nameof(Test_Field_ValueType_Instance_Read):
                    Test_Field_ValueType_Instance_Read();
                    break;
                case nameof(Test_Field_ValueType_Instance_Write):
                    Test_Field_ValueType_Instance_Write();
                    break;
                case nameof(Test_Field_ReferenceType_Instance_Read):
                    Test_Field_ReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_ReferenceType_Instance_Write):
                    Test_Field_ReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_ValueType_OnValueType_Instance_Read):
                    Test_Field_ValueType_OnValueType_Instance_Read();
                    break;
                case nameof(Test_Field_ValueType_OnValueType_Instance_Write):
                    Test_Field_ValueType_OnValueType_Instance_Write();
                    break;
                case nameof(Test_Field_ReferenceType_OnValueType_Instance_Read):
                    Test_Field_ReferenceType_OnValueType_Instance_Read();
                    break;
                case nameof(Test_Field_ReferenceType_OnValueType_Instance_Write):
                    Test_Field_ReferenceType_OnValueType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromType_ValueType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromType_ReferenceType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromMethod_ValueType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromMethod_ReferenceType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromBoth_ValueType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromBoth_ReferenceType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Read):
                    Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Write):
                    Test_Field_Generic_FromType_ValueType_OnValueType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Read):
                    Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Write):
                    Test_Field_Generic_FromType_ReferenceType_OnValueType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromType_ArrayOfValueType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromType_ArrayOfReferenceType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromType_NestedGeneric_ValueType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_FromType_NestedGeneric_ReferenceType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_MultiParam_ValueType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Read):
                    Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Read();
                    break;
                case nameof(Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Write):
                    Test_Field_Generic_MultiParam_ReferenceType_OnReferenceType_Instance_Write();
                    break;
                case nameof(Test_Field_ValueType_Static_Read):
                    Test_Field_ValueType_Static_Read();
                    break;
                case nameof(Test_Field_ValueType_Static_Write):
                    Test_Field_ValueType_Static_Write();
                    break;
                case nameof(Test_Field_ReferenceType_Static_Read):
                    Test_Field_ReferenceType_Static_Read();
                    break;
                case nameof(Test_Field_ReferenceType_Static_Write):
                    Test_Field_ReferenceType_Static_Write();
                    break;
                // Properties
                case nameof(Test_Property_ValueType_Instance_Read):
                    Test_Property_ValueType_Instance_Read();
                    break;
                case nameof(Test_Property_ValueType_Instance_Write):
                    Test_Property_ValueType_Instance_Write();
                    break;
                case nameof(Test_Property_ReferenceType_Instance_Read):
                    Test_Property_ReferenceType_Instance_Read();
                    break;
                case nameof(Test_Property_ReferenceType_Instance_Write):
                    Test_Property_ReferenceType_Instance_Write();
                    break;
                case nameof(Test_Property_ValueType_Static_Read):
                    Test_Property_ValueType_Static_Read();
                    break;
                case nameof(Test_Property_ValueType_Static_Write):
                    Test_Property_ValueType_Static_Write();
                    break;
                case nameof(Test_Property_ReferenceType_Static_Read):
                    Test_Property_ReferenceType_Static_Read();
                    break;
                case nameof(Test_Property_ReferenceType_Static_Write):
                    Test_Property_ReferenceType_Static_Write();
                    break;
                // Volatile fields
                case nameof(Test_Field_Volatile_ValueType_Static_Read):
                    Test_Field_Volatile_ValueType_Static_Read();
                    break;
                case nameof(Test_Field_Volatile_ValueType_Static_Write):
                    Test_Field_Volatile_ValueType_Static_Write();
                    break;
                case nameof(Test_Field_Volatile_ValueType_Instance_Read):
                    Test_Field_Volatile_ValueType_Instance_Read();
                    break;
                case nameof(Test_Field_Volatile_ValueType_Instance_Write):
                    Test_Field_Volatile_ValueType_Instance_Write();
                    break;
                case nameof(Test_Field_ValueType_Static_TernaryWrite):
                    Test_Field_ValueType_Static_TernaryWrite();
                    break;
                case nameof(Test_Field_Volatile_ValueType_Static_TernaryWrite):
                    Test_Field_Volatile_ValueType_Static_TernaryWrite();
                    break;
                case nameof(Test_Field_ReferenceType_Instance_TernaryValueWrite):
                    Test_Field_ReferenceType_Instance_TernaryValueWrite();
                    break;
                case nameof(Test_Field_ReferenceType_Instance_TernaryReceiverRead):
                    Test_Field_ReferenceType_Instance_TernaryReceiverRead();
                    break;
            }

            // Array events
            switch (args[0])
            {
                case nameof(Test_Array_I_Read):
                    Test_Array_I_Read();
                    break;
                case nameof(Test_Array_I_Write):
                    Test_Array_I_Write();
                    break;
                case nameof(Test_Array_I1_Read):
                    Test_Array_I1_Read();
                    break;
                case nameof(Test_Array_I1_Write):
                    Test_Array_I1_Write();
                    break;
                case nameof(Test_Array_I2_Read):
                    Test_Array_I2_Read();
                    break;
                case nameof(Test_Array_I2_Write):
                    Test_Array_I2_Write();
                    break;
                case nameof(Test_Array_I4_Read):
                    Test_Array_I4_Read();
                    break;
                case nameof(Test_Array_I4_Write):
                    Test_Array_I4_Write();
                    break;
                case nameof(Test_Array_I8_Read):
                    Test_Array_I8_Read();
                    break;
                case nameof(Test_Array_I8_Write):
                    Test_Array_I8_Write();
                    break;
                case nameof(Test_Array_U1_Read):
                    Test_Array_U1_Read();
                    break;
                case nameof(Test_Array_U1_Write):
                    Test_Array_U1_Write();
                    break;
                case nameof(Test_Array_U2_Read):
                    Test_Array_U2_Read();
                    break;
                case nameof(Test_Array_U2_Write):
                    Test_Array_U2_Write();
                    break;
                case nameof(Test_Array_U4_Read):
                    Test_Array_U4_Read();
                    break;
                case nameof(Test_Array_U4_Write):
                    Test_Array_U4_Write();
                    break;
                case nameof(Test_Array_U8_Read):
                    Test_Array_U8_Read();
                    break;
                case nameof(Test_Array_U8_Write):
                    Test_Array_U8_Write();
                    break;
                case nameof(Test_Array_R4_Read):
                    Test_Array_R4_Read();
                    break;
                case nameof(Test_Array_R4_Write):
                    Test_Array_R4_Write();
                    break;
                case nameof(Test_Array_R8_Read):
                    Test_Array_R8_Read();
                    break;
                case nameof(Test_Array_R8_Write):
                    Test_Array_R8_Write();
                    break;
                case nameof(Test_Array_Ref_Read):
                    Test_Array_Ref_Read();
                    break;
                case nameof(Test_Array_Ref_Write):
                    Test_Array_Ref_Write();
                    break;
                case nameof(Test_Array_Struct_Read):
                    Test_Array_Struct_Read();
                    break;
                case nameof(Test_Array_Struct_Write):
                    Test_Array_Struct_Write();
                    break;
            }

            // Concurrency issues detection
            switch (args[0])
            {
                case nameof(Test_NoDeadlock):
                    Test_NoDeadlock();
                    break;
                case nameof(Test_Deadlock_SimpleDeadlock_UsingMonitor):
                    Test_Deadlock_SimpleDeadlock_UsingMonitor();
                    break;
#if NET9_0_OR_GREATER
                case nameof(Test_Deadlock_SimpleDeadlock_UsingLock):
                    Test_Deadlock_SimpleDeadlock_UsingLock();
                    break;
#endif
                case nameof(Test_Deadlock_ThreadJoinDeadlock):
                    Test_Deadlock_ThreadJoinDeadlock();
                    break;
                case nameof(Test_Deadlock_MixedMonitorAndThreadJoinDeadlock):
                    Test_Deadlock_MixedMonitorAndThreadJoinDeadlock();
                    break;
                case nameof(Test_DataRace_ReferenceType_Static_ReadWriteRace):
                    Test_DataRace_ReferenceType_Static_ReadWriteRace();
                    break;
                case nameof(Test_DataRace_ValueType_Static_ReadWriteRace):
                    Test_DataRace_ValueType_Static_ReadWriteRace();
                    break;
                case nameof(Test_DataRace_DeepStack_HelperWriteRace):
                    Test_DataRace_DeepStack_HelperWriteRace();
                    break;
                case nameof(Test_DataRace_ReferenceType_Instance_ReadWriteRace):
                    Test_DataRace_ReferenceType_Instance_ReadWriteRace();
                    break;
                case nameof(Test_DataRace_ValueType_Instance_ReadWriteRace):
                    Test_DataRace_ValueType_Instance_ReadWriteRace();
                    break;
                case nameof(Test_DataRace_ReferenceType_Static_WriteReadRace):
                    Test_DataRace_ReferenceType_Static_WriteReadRace();
                    break;
                case nameof(Test_DataRace_ValueType_Static_WriteReadRace):
                    Test_DataRace_ValueType_Static_WriteReadRace();
                    break;
                case nameof(Test_DataRace_ReferenceType_Instance_WriteReadRace):
                    Test_DataRace_ReferenceType_Instance_WriteReadRace();
                    break;
                case nameof(Test_DataRace_ValueType_Instance_WriteReadRace):
                    Test_DataRace_ValueType_Instance_WriteReadRace();
                    break;
                case nameof(Test_DataRace_ReferenceType_Instance_WriteWriteRace):
                    Test_DataRace_ReferenceType_Instance_WriteWriteRace();
                    break;
                case nameof(Test_DataRace_ValueType_Instance_WriteWriteRace):
                    Test_DataRace_ValueType_Instance_WriteWriteRace();
                    break;
                case nameof(Test_DataRace_ReferenceType_Instance_SingleWriterWriteReadRace):
                    Test_DataRace_ReferenceType_Instance_SingleWriterWriteReadRace();
                    break;
                case nameof(Test_DataRace_ValueType_Instance_SingleWriterWriteReadRace):
                    Test_DataRace_ValueType_Instance_SingleWriterWriteReadRace();
                    break;
                case nameof(Test_DataRace_AutoProperty_Instance_PostPublicationWriteReadRace):
                    Test_DataRace_AutoProperty_Instance_PostPublicationWriteReadRace();
                    break;
                case nameof(Test_DataRace_Static_WrittenInInstanceCtor_WriteReadRace):
                    Test_DataRace_Static_WrittenInInstanceCtor_WriteReadRace();
                    break;
                case nameof(Test_DataRace_Static_AutoProperty_WriteReadRace):
                    Test_DataRace_Static_AutoProperty_WriteReadRace();
                    break;
                case nameof(Test_NoDataRace_ConstructorWrite_PublishThenRead):
                    Test_NoDataRace_ConstructorWrite_PublishThenRead();
                    break;
                case nameof(Test_NoDataRace_ConstructorAutoPropertyWrite_PublishThenRead):
                    Test_NoDataRace_ConstructorAutoPropertyWrite_PublishThenRead();
                    break;
                case nameof(Test_NoDataRace_StaticCctorHelperWrite_ConcurrentFirstAccess):
                    Test_NoDataRace_StaticCctorHelperWrite_ConcurrentFirstAccess();
                    break;
                case nameof(Test_NoDataRace_ConcurrentDictionaryPublishThenRead):
                    Test_NoDataRace_ConcurrentDictionaryPublishThenRead();
                    break;
                case nameof(Test_NoDataRace_ConcurrentDictionaryGetOrAddFactoryThenRead):
                    Test_NoDataRace_ConcurrentDictionaryGetOrAddFactoryThenRead();
                    break;
                case nameof(Test_NoDataRace_ConcurrentDictionaryMissingKeyThrows):
                    Test_NoDataRace_ConcurrentDictionaryMissingKeyThrows();
                    break;
                case nameof(Test_DataRace_ConcurrentDictionaryPostPublicationWrite):
                    Test_DataRace_ConcurrentDictionaryPostPublicationWrite();
                    break;
                case nameof(Test_NoDataRace_LazyPublishThenRead):
                    Test_NoDataRace_LazyPublishThenRead();
                    break;
                case nameof(Test_NoDataRace_LazyValueTypePublishThenRead):
                    Test_NoDataRace_LazyValueTypePublishThenRead();
                    break;
                case nameof(Test_NoDataRace_LazyFactoryThrows):
                    Test_NoDataRace_LazyFactoryThrows();
                    break;
                case nameof(Test_DataRace_LazyPostPublicationWrite):
                    Test_DataRace_LazyPostPublicationWrite();
                    break;
                case nameof(Test_DataRace_StaticHelperWrite_NotFromCctor_WriteReadRace):
                    Test_DataRace_StaticHelperWrite_NotFromCctor_WriteReadRace();
                    break;
                case nameof(Test_NoDataRace_CtorSetterWrite_PublishThenRead):
                    Test_NoDataRace_CtorSetterWrite_PublishThenRead();
                    break;
                case nameof(Test_DataRace_ObjectInitializerThenPostPublicationWrite):
                    Test_DataRace_ObjectInitializerThenPostPublicationWrite();
                    break;
                case nameof(Test_NoDataRace_ReferenceType_Static_ReadReadNoRace):
                    Test_NoDataRace_ReferenceType_Static_ReadReadNoRace();
                    break;
                case nameof(Test_NoDataRace_ValueType_Static_ReadReadNoRace):
                    Test_NoDataRace_ValueType_Static_ReadReadNoRace();
                    break;
                case nameof(Test_NoDataRace_ReferenceType_Instance_ReadReadNoRace):
                    Test_NoDataRace_ReferenceType_Instance_ReadReadNoRace();
                    break;
                case nameof(Test_NoDataRace_ValueType_Instance_ReadReadNoRace):
                    Test_NoDataRace_ValueType_Instance_ReadReadNoRace();
                    break;
                case nameof(Test_NoDataRace_ThreadStatic_ReferenceType):
                    Test_NoDataRace_ThreadStatic_ReferenceType();
                    break;
                case nameof(Test_NoDataRace_ThreadStatic_ValueType):
                    Test_NoDataRace_ThreadStatic_ValueType();
                    break;
                case nameof(Test_NoDataRace_ThreadStatic_ReadWrite):
                    Test_NoDataRace_ThreadStatic_ReadWrite();
                    break;
                case nameof(Test_NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace):
                    Test_NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace();
                    break;
                case nameof(Test_NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace):
                    Test_NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace();
                    break;
                case nameof(Test_NoDataRace_VolatileField_Static_ReadWriteNoRace):
                    Test_NoDataRace_VolatileField_Static_ReadWriteNoRace();
                    break;
                case nameof(Test_NoDataRace_VolatileField_Instance_ReadWriteNoRace):
                    Test_NoDataRace_VolatileField_Instance_ReadWriteNoRace();
                    break;
                case nameof(Test_NoDataRace_VolatilePingPong_OrdersPlainFieldAccesses):
                    Test_NoDataRace_VolatilePingPong_OrdersPlainFieldAccesses();
                    break;
                case nameof(Test_NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin):
                    Test_NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin();
                    break;
                case nameof(Test_NoDataRace_Task_SequentialTasks_WriteRead):
                    Test_NoDataRace_Task_SequentialTasks_WriteRead();
                    break;
                case nameof(Test_NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace):
                    Test_NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace();
                    break;
                case nameof(Test_NoDataRace_GenericType_StaticInitializer_DifferentInstantiations_WriteWrite_NoRace):
                    Test_NoDataRace_GenericType_StaticInitializer_DifferentInstantiations_WriteWrite_NoRace();
                    break;
                case nameof(Test_NoDataRace_SemaphoreSlim_ProtectedWriteRead):
                    Test_NoDataRace_SemaphoreSlim_ProtectedWriteRead();
                    break;
                case nameof(Test_NoDataRace_SemaphoreSlimAsync_ProtectedWriteRead):
                    Test_NoDataRace_SemaphoreSlimAsync_ProtectedWriteRead();
                    break;
                case nameof(Test_NoDataRace_SemaphoreSlimAsync_HighContention_WriteRead):
                    Test_NoDataRace_SemaphoreSlimAsync_HighContention_WriteRead();
                    break;
                case nameof(Test_NoDataRace_SemaphoreSlimAsync_WithCancellationToken_ProtectedWriteRead):
                    Test_NoDataRace_SemaphoreSlimAsync_WithCancellationToken_ProtectedWriteRead();
                    break;
                case nameof(Test_NoDataRace_SemaphoreSlimAsync_WithTimeout_ProtectedWriteRead):
                    Test_NoDataRace_SemaphoreSlimAsync_WithTimeout_ProtectedWriteRead();
                    break;
                case nameof(Test_NoDataRace_SemaphoreSlimAsync_CanceledWait_NoSharedAccess):
                    Test_NoDataRace_SemaphoreSlimAsync_CanceledWait_NoSharedAccess();
                    break;
                case nameof(Test_NoDataRace_SemaphoreSlimAsync_TimeoutExpires_NoSharedAccess):
                    Test_NoDataRace_SemaphoreSlimAsync_TimeoutExpires_NoSharedAccess();
                    break;
                case nameof(Test_NoDataRace_Monitor_HighContention_WriteRead):
                    Test_NoDataRace_Monitor_HighContention_WriteRead();
                    break;
                case nameof(Test_NoDataRace_Semaphore_HighContention_WriteRead):
                    Test_NoDataRace_Semaphore_HighContention_WriteRead();
                    break;
                case nameof(Test_NoDataRace_Semaphore_BatchRelease_WriteRead):
                    Test_NoDataRace_Semaphore_BatchRelease_WriteRead();
                    break;
                case nameof(Test_NoDataRace_Mutex_ProtectedWriteRead):
                    Test_NoDataRace_Mutex_ProtectedWriteRead();
                    break;
                case nameof(Test_NoDataRace_Mutex_HighContention_WriteRead):
                    Test_NoDataRace_Mutex_HighContention_WriteRead();
                    break;
                case nameof(Test_NoDataRace_KernelSemaphore_ProtectedWriteRead):
                    Test_NoDataRace_KernelSemaphore_ProtectedWriteRead();
                    break;
                case nameof(Test_NoDataRace_AutoResetEvent_WriteThenSet_WaitThenRead):
                    Test_NoDataRace_AutoResetEvent_WriteThenSet_WaitThenRead();
                    break;
                case nameof(Test_NoDataRace_ManualResetEvent_PublishThenRead):
                    Test_NoDataRace_ManualResetEvent_PublishThenRead();
                    break;
                case nameof(Test_DataRace_ManualResetEvent_SetBeforeWrite_WriteReadRace):
                    Test_DataRace_ManualResetEvent_SetBeforeWrite_WriteReadRace();
                    break;
                case nameof(Test_NoDataRace_SignalAndWait_PingPong):
                    Test_NoDataRace_SignalAndWait_PingPong();
                    break;
                case nameof(Test_NoDataRace_AbandonedMutex_WaiterStillOrdered):
                    Test_NoDataRace_AbandonedMutex_WaiterStillOrdered();
                    break;
                case nameof(Test_NoDataRace_WaitAll_TwoEvents_JoinsBothPublishers):
                    Test_NoDataRace_WaitAll_TwoEvents_JoinsBothPublishers();
                    break;
                case nameof(Test_NoDataRace_WaitAny_WinnerOrdersAccess):
                    Test_NoDataRace_WaitAny_WinnerOrdersAccess();
                    break;
                case nameof(Test_DataRace_WaitAny_LoserNotOrdered):
                    Test_DataRace_WaitAny_LoserNotOrdered();
                    break;
                case nameof(Test_NoDataRace_WaitAny_AbandonedMutex_WaiterStillOrdered):
                    Test_NoDataRace_WaitAny_AbandonedMutex_WaiterStillOrdered();
                    break;
            }

            // Disposables
            switch (args[0])
            {
                case nameof(Test_NonDisposedAnalysis_CustomDisposable_Disposed):
                    Test_NonDisposedAnalysis_CustomDisposable_Disposed();
                    break;
                case nameof(Test_NonDisposedAnalysis_CustomDisposable_NonDisposed):
                    Test_NonDisposedAnalysis_CustomDisposable_NonDisposed();
                    break;
            }

            // Multi-process scenarios
            switch (args[0])
            {
                case nameof(Test_MultiProcess_ChildExitsBeforeParent):
                    Test_MultiProcess_ChildExitsBeforeParent();
                    break;
                case nameof(Test_MultiProcess_Child_LockAndExit):
                    Test_MultiProcess_Child_LockAndExit();
                    break;
            }

            // Garbage collection events
            switch (args[0])
            {
                case nameof(Test_SingleGarbageCollection_ObjectTracking_Simple):
                    Test_SingleGarbageCollection_ObjectTracking_Simple();
                    break;
                case nameof(Test_MultipleGarbageCollection_ObjectTracking_Simple):
                    Test_MultipleGarbageCollection_ObjectTracking_Simple();
                    break;
                case nameof(Test_SingleGarbageCollection_ObjectTracking_MovedLockedObject):
                    Test_SingleGarbageCollection_ObjectTracking_MovedLockedObject();
                    break;
            }

            // IL verification
            switch (args[0])
            {
                case nameof(Verify_ArrayElementAccess_Instrumentation):
                    Verify_ArrayElementAccess_Instrumentation();
                    break;
                case nameof(Verify_FieldElementAccess_Instrumentation):
                    Verify_FieldElementAccess_Instrumentation();
                    break;
            }
        }
    }
}
