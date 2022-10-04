using SharpDetect.E2ETests.Subject.Helpers.Arrays;
using SharpDetect.E2ETests.Subject.Helpers.DataRaces;
using SharpDetect.E2ETests.Subject.Helpers.Fields;

namespace SharpDetect.E2ETests.Subject
{
    public static class Program
    {
        public static void Test_MonitorMethods_EnterExit1()
        {
            lock (new object()) { }
        }

        public static void Test_MonitorMethods_EnterExit2()
        {
            var obj = new object();
            Monitor.Enter(obj);
            Monitor.Exit(obj);
        }

        public static void Test_MonitorMethods_TryEnterExit1()
        {
            var obj = new object();
            Monitor.TryEnter(obj);
            Monitor.Exit(obj);
        }

        public static void Test_MonitorMethods_TryEnterExit2()
        {
            var obj = new object();
            Monitor.TryEnter(obj, TimeSpan.FromSeconds(1));
            Monitor.Exit(obj);
        }

        public static void Test_MonitorMethods_TryEnterExit3()
        {
            var obj = new object();
            var lockTaken = false;
            Monitor.TryEnter(obj, TimeSpan.FromSeconds(1), ref lockTaken);
            if (lockTaken)
                Monitor.Exit(obj);
        }

        public static void Test_Field_ValueType_Instance_Read()
        {
            var cpy = new InstanceFieldValueType().Test_Field_ValueType_Instance;
        }

        public static void Test_Field_ValueType_Instance_Write()
        {
            var instance = new InstanceFieldValueType();
            instance.Test_Field_ValueType_Instance = 123;
        }

        public static void Test_Field_ReferenceType_Instance_Read()
        {
            var cpy = new InstanceFieldReferenceType().Test_Field_ReferenceType_Instance;
        }

        public static void Test_Field_ReferenceType_Instance_Write()
        {
            var instance = new InstanceFieldReferenceType();
            instance.Test_Field_ReferenceType_Instance = new object();
        }

        public static void Test_Field_ValueType_Static_Read()
        {
            var cpy = StaticFieldValueType.Test_Field_ValueType_Static;
        }

        public static void Test_Field_ValueType_Static_Write()
        {
            StaticFieldValueType.Test_Field_ValueType_Static = 123;
        }

        public static void Test_Field_ReferenceType_Static_Read()
        {
            var cpy = StaticFieldReferenceType.Test_Field_ReferenceType_Static;
        }

        public static void Test_Field_ReferenceType_Static_Write()
        {
            StaticFieldReferenceType.Test_Field_ReferenceType_Static = new object();
        }

        public static void Test_Array_I_Read()
        {
            ArrayElement.Test_Array_I = new nuint[1];
            var cpy = ArrayElement.Test_Array_I[0];
        }

        public static void Test_Array_I_Write()
        {
            ArrayElement.Test_Array_I = new nuint[1];
            ArrayElement.Test_Array_I[0] = 123;
        }

        public static void Test_Array_I1_Read()
        {
            ArrayElement.Test_Array_I1 = new sbyte[1];
            var cpy = ArrayElement.Test_Array_I1[0];
        }

        public static void Test_Array_I1_Write()
        {
            ArrayElement.Test_Array_I1 = new sbyte[1];
            ArrayElement.Test_Array_I1[0] = 123;
        }

        public static void Test_Array_I2_Read()
        {
            ArrayElement.Test_Array_I2 = new short[1];
            var cpy = ArrayElement.Test_Array_I2[0];
        }

        public static void Test_Array_I2_Write()
        {
            ArrayElement.Test_Array_I2 = new short[1];
            ArrayElement.Test_Array_I2[0] = 123;
        }

        public static void Test_Array_I4_Read()
        {
            ArrayElement.Test_Array_I4 = new int[1];
            var cpy = ArrayElement.Test_Array_I4[0];
        }

        public static void Test_Array_I4_Write()
        {
            ArrayElement.Test_Array_I4 = new int[1];
            ArrayElement.Test_Array_I4[0] = 123;
        }

        public static void Test_Array_I8_Read()
        {
            ArrayElement.Test_Array_I8 = new long[1];
            var cpy = ArrayElement.Test_Array_I8[0];
        }

        public static void Test_Array_I8_Write()
        {
            ArrayElement.Test_Array_I8 = new long[1];
            ArrayElement.Test_Array_I8[0] = 123;
        }

        public static void Test_Array_U1_Read()
        {
            ArrayElement.Test_Array_U1 = new byte[1];
            var cpy = ArrayElement.Test_Array_U1[0];
        }

        public static void Test_Array_U1_Write()
        {
            ArrayElement.Test_Array_U1 = new byte[1];
            ArrayElement.Test_Array_U1[0] = 123;
        }

        public static void Test_Array_U2_Read()
        {
            ArrayElement.Test_Array_U2 = new ushort[1];
            var cpy = ArrayElement.Test_Array_U2[0];
        }

        public static void Test_Array_U2_Write()
        {
            ArrayElement.Test_Array_U2 = new ushort[1];
            ArrayElement.Test_Array_U2[0] = 123;
        }

        public static void Test_Array_U4_Read()
        {
            ArrayElement.Test_Array_U4 = new uint[1];
            var cpy = ArrayElement.Test_Array_U4[0];
        }

        public static void Test_Array_U4_Write()
        {
            ArrayElement.Test_Array_U4 = new uint[1];
            ArrayElement.Test_Array_U4[0] = 123;
        }

        public static void Test_Array_U8_Read()
        {
            ArrayElement.Test_Array_U8 = new ulong[1];
            var cpy = ArrayElement.Test_Array_U8[0];
        }

        public static void Test_Array_U8_Write()
        {
            ArrayElement.Test_Array_U8 = new ulong[1];
            ArrayElement.Test_Array_U8[0] = 123;
        }

        public static void Test_Array_R4_Read()
        {
            ArrayElement.Test_Array_R4 = new float[1];
            var cpy = ArrayElement.Test_Array_R4[0];
        }

        public static void Test_Array_R4_Write()
        {
            ArrayElement.Test_Array_R4 = new float[1];
            ArrayElement.Test_Array_R4[0] = 123;
        }

        public static void Test_Array_R8_Read()
        {
            ArrayElement.Test_Array_R8 = new double[1];
            var cpy = ArrayElement.Test_Array_R8[0];
        }

        public static void Test_Array_R8_Write()
        {
            ArrayElement.Test_Array_R8 = new double[1];
            ArrayElement.Test_Array_R8[0] = 123;
        }

        public static void Test_Array_Ref_Read()
        {
            ArrayElement.Test_Array_Ref = new object[1];
            var cpy = ArrayElement.Test_Array_Ref[0];
        }

        public static void Test_Array_Ref_Write()
        {
            ArrayElement.Test_Array_Ref = new object[1];
            ArrayElement.Test_Array_Ref[0] = 123;
        }

        public static void Test_Array_Struct_Read()
        {
            ArrayElement.Test_Array_Struct = new DateTime[1];
            var cpy = ArrayElement.Test_Array_Struct[0];
        }

        public static void Test_Array_Struct_Write()
        {
            ArrayElement.Test_Array_Struct = new DateTime[1];
            ArrayElement.Test_Array_Struct[0] = DateTime.UtcNow;
        }

        public static void Test_DataRace_ReferenceType_Static_SimpleRace()
        {
            var task1 = Task.Run(() => DataRace.Test_DataRace_ReferenceType_Static = new object());
            var task2 = Task.Run(() => DataRace.Test_DataRace_ReferenceType_Static = new object());
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ReferenceType_Instance_SimpleRace()
        {
            var instance = new DataRace();
            var task1 = Task.Run(() => instance.Test_DataRace_ReferenceType_Instance = new object());
            var task2 = Task.Run(() => instance.Test_DataRace_ReferenceType_Instance = new object());
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ValueType_Static_SimpleRace()
        {
            var task1 = Task.Run(() => DataRace.Test_DataRace_ValueType_Static = 123);
            var task2 = Task.Run(() => DataRace.Test_DataRace_ValueType_Static = 321);
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ValueType_Instance_SimpleRace()
        {
            var instance = new DataRace();
            var task1 = Task.Run(() => instance.Test_DataRace_ValueType_Instance = 123);
            var task2 = Task.Run(() => instance.Test_DataRace_ValueType_Instance = 321);
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ReferenceType_Static_BadLocking()
        {
            var lockObj = new object();
            var otherLock = new object();
            var task1 = Task.Run(() => { lock (lockObj) { DataRace.Test_DataRace_ReferenceType_Static = new object(); } });
            var task2 = Task.Run(() => { lock (otherLock) { DataRace.Test_DataRace_ReferenceType_Static = new object(); } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ValueType_Static_BadLocking()
        {
            var lockObj = new object();
            var otherLock = new object();
            var task1 = Task.Run(() => { lock (lockObj) { DataRace.Test_DataRace_ValueType_Static = 123; } });
            var task2 = Task.Run(() => { lock (otherLock) { DataRace.Test_DataRace_ValueType_Static = 321; } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ReferenceType_Static_CorrectLocks()
        {
            var lockObj = new object();
            var task1 = Task.Run(() => { lock (lockObj) { DataRace.Test_DataRace_ReferenceType_Static = new object(); } });
            var task2 = Task.Run(() => { lock (lockObj) { DataRace.Test_DataRace_ReferenceType_Static = new object(); } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ReferenceType_Instance_CorrectLocks()
        {
            var instance = new DataRace();
            var lockObj = new object();
            var task1 = Task.Run(() => { lock (lockObj) { instance.Test_DataRace_ReferenceType_Instance = new object(); } });
            var task2 = Task.Run(() => { lock (lockObj) { instance.Test_DataRace_ReferenceType_Instance = new object(); } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ValueType_Static_CorrectLocks()
        {
            var lockObj = new object();
            var task1 = Task.Run(() => { lock (lockObj) { DataRace.Test_DataRace_ValueType_Static = 123; } });
            var task2 = Task.Run(() => { lock (lockObj) { DataRace.Test_DataRace_ValueType_Static = 321; } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ValueType_Instance_CorrectLocks()
        {
            var instance = new DataRace();
            var lockObj = new object();
            var task1 = Task.Run(() => { lock (lockObj) { instance.Test_DataRace_ValueType_Instance = 123; } });
            var task2 = Task.Run(() => { lock (lockObj) { instance.Test_DataRace_ValueType_Instance = 321; } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ReferenceType_Instance_BadLocking()
        {
            var instance = new DataRace();
            var lockObj = new object();
            var otherLock = new object();
            var task1 = Task.Run(() => { lock (lockObj) { instance.Test_DataRace_ReferenceType_Instance = new object(); } });
            var task2 = Task.Run(() => { lock (otherLock) { instance.Test_DataRace_ReferenceType_Instance = new object(); } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_DataRace_ValueType_Instance_BadLocking()
        {
            var instance = new DataRace();
            var lockObj = new object();
            var otherLock = new object();
            var task1 = Task.Run(() => { lock (lockObj) { instance.Test_DataRace_ValueType_Instance = 123; } });
            var task2 = Task.Run(() => { lock (otherLock) { instance.Test_DataRace_ValueType_Instance = 321; } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ReferenceType_Instance_DifferentInstances()
        {
            var instance1 = new DataRace();
            var instance2 = new DataRace();
            var lockObj = new object();
            var task1 = Task.Run(() => { lock (lockObj) { instance1.Test_DataRace_ValueType_Instance = 123; } });
            var task2 = Task.Run(() => { lock (lockObj) { instance2.Test_DataRace_ValueType_Instance = 321; } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_ValueType_Instance_DifferentInstances()
        {
            var instance1 = new DataRace();
            var instance2 = new DataRace();
            var lockObj = new object();
            var task1 = Task.Run(() => { lock (lockObj) { instance1.Test_DataRace_ReferenceType_Instance = new object(); } });
            var task2 = Task.Run(() => { lock (lockObj) { instance2.Test_DataRace_ReferenceType_Instance = new object(); } });
            Task.WaitAll(task1, task2);
        }

        public static void Test_GarbageCollection_Simple()
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

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

            // Data-race on fields detection
            switch (args[0])
            {
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
                case nameof(Test_GarbageCollection_Simple):
                    Test_GarbageCollection_Simple();
                    break;
            }
        }
    }
}