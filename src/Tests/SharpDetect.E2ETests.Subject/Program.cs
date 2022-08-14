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


        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

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
        }
    }
}