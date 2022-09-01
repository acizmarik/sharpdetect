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


        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            switch (args[0])
            {
                // Locks
                case nameof(Test_MonitorMethods_EnterExit1):
                    Test_MonitorMethods_EnterExit1();
                    break;
                case nameof(Test_MonitorMethods_EnterExit2):
                    Test_MonitorMethods_EnterExit2();
                    break;
                case nameof(Test_MonitorMethods_TryEnterExit1):
                    Test_MonitorMethods_TryEnterExit1 ();
                    break;
                case nameof(Test_MonitorMethods_TryEnterExit2):
                    Test_MonitorMethods_TryEnterExit2();
                    break;
                case nameof(Test_MonitorMethods_TryEnterExit3):
                    Test_MonitorMethods_TryEnterExit3();
                    break;
                // Fields
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
        }
    }
}