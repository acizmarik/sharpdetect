namespace SharpDetect.UnitTests.DnlibExtensions
{
    internal class AssemblerTestMethods
    {
        public static string EmptyString()
        {
            return "";
        }

        public static void DistantBranchTarget(int num)
        {
            if (num == 123)
            {
                Console.WriteLine("1");
                Console.WriteLine("2");
                Console.WriteLine("3");
                Console.WriteLine("4");
                Console.WriteLine("5");
                Console.WriteLine("6");
                Console.WriteLine("7");
                Console.WriteLine("8");
                Console.WriteLine("9");
                Console.WriteLine("10");
                Console.WriteLine("11");
                Console.WriteLine("12");
                Console.WriteLine("13");
                Console.WriteLine("14");
                Console.WriteLine("15");
            }
        }
    }
}
