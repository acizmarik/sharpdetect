// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.E2ETests.Subject
{
    public static partial class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

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
                case nameof(Test_Deadlock_SimpleDeadlock):
                    Test_Deadlock_SimpleDeadlock();
                    break;
                case nameof(Test_DataRace_ReferenceType_Static_SimpleRace):
                    Test_DataRace_ReferenceType_Static_SimpleRace();
                    break;
                case nameof(Test_DataRace_ValueType_Static_SimpleRace):
                    Test_DataRace_ValueType_Static_SimpleRace();
                    break;
                case nameof(Test_DataRace_ReferenceType_Static_BadLocking):
                    Test_DataRace_ReferenceType_Static_BadLocking();
                    break;
                case nameof(Test_DataRace_ValueType_Static_BadLocking):
                    Test_DataRace_ValueType_Static_BadLocking();
                    break;
                case nameof(Test_NoDataRace_ReferenceType_Static_CorrectLocks):
                    Test_NoDataRace_ReferenceType_Static_CorrectLocks();
                    break;
                case nameof(Test_NoDataRace_ValueType_Static_CorrectLocks):
                    Test_NoDataRace_ValueType_Static_CorrectLocks();
                    break;
                case nameof(Test_DataRace_ReferenceType_Instance_SimpleRace):
                    Test_DataRace_ReferenceType_Instance_SimpleRace();
                    break;
                case nameof(Test_DataRace_ValueType_Instance_SimpleRace):
                    Test_DataRace_ValueType_Instance_SimpleRace();
                    break;
                case nameof(Test_NoDataRace_ReferenceType_Instance_CorrectLocks):
                    Test_NoDataRace_ReferenceType_Instance_CorrectLocks();
                    break;
                case nameof(Test_NoDataRace_ValueType_Instance_CorrectLocks):
                    Test_NoDataRace_ValueType_Instance_CorrectLocks();
                    break;
                case nameof(Test_DataRace_ReferenceType_Instance_BadLocking):
                    Test_DataRace_ReferenceType_Instance_BadLocking();
                    break;
                case nameof(Test_DataRace_ValueType_Instance_BadLocking):
                    Test_DataRace_ValueType_Instance_BadLocking();
                    break;
                case nameof(Test_NoDataRace_ReferenceType_Instance_DifferentInstances):
                    Test_NoDataRace_ReferenceType_Instance_DifferentInstances();
                    break;
                case nameof(Test_NoDataRace_ValueType_Instance_DifferentInstances):
                    Test_NoDataRace_ValueType_Instance_DifferentInstances();
                    break;
            }

            // Garbage collection events
            switch (args[0])
            {
                case nameof(Test_SingleGarbageCollection_NonCompacting_Simple):
                    Test_SingleGarbageCollection_NonCompacting_Simple();
                    break;
                case nameof(Test_SingleGarbageCollection_Compacting_Simple):
                    Test_SingleGarbageCollection_Compacting_Simple();
                    break;
                case nameof(Test_SingleGarbageCollection_ObjectTracking_Simple):
                    Test_SingleGarbageCollection_ObjectTracking_Simple();
                    break;
                case nameof(Test_MultipleGarbageCollection_NonCompacting_Simple):
                    Test_MultipleGarbageCollection_NonCompacting_Simple();
                    break;
                case nameof(Test_MultipleGarbageCollection_Compacting_Simple):
                    Test_MultipleGarbageCollection_Compacting_Simple();
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
