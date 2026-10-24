// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.E2ETests.Subject.Helpers.DataRaces;

namespace SharpDetect.E2ETests.Subject.Helpers.Fields
{
    public class StaticFieldValueType
    {
        public static int Test_Field_ValueType_Static;
        public static volatile int Test_Field_Volatile_ValueType_Static;
        public static int Test_Field_Atomic_ValueType_Static;
        public static long Test_Field_Atomic_ValueType_Static_Long;
        public static double Test_Field_Atomic_ValueType_Static_Double;
        public static float Test_Field_Atomic_ValueType_Static_Single;
        public static AtomicFlagState Test_Field_Atomic_ValueType_Static_Enum;
        public static AtomicFlag Test_Field_Atomic_ValueType_Static_Struct;
        public static int Test_Property_ValueType_Static { get; set; }
    }
}