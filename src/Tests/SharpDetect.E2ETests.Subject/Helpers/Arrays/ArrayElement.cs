namespace SharpDetect.E2ETests.Subject.Helpers.Arrays
{
    public class ArrayElement
    {
        // (LD/ST)ELEM_I
        public static nuint[]? Test_Array_I;

        // (LD/ST)ELEM_I1
        public static sbyte[]? Test_Array_I1;
        // (LD/ST)ELEM_I2
        public static short[]? Test_Array_I2;
        // (LD/ST)ELEM_I4
        public static int[]? Test_Array_I4;
        // (LD/ST)ELEM_I8
        public static long[]? Test_Array_I8;

        // (LD/ST)ELEM_U1
        public static byte[]? Test_Array_U1;
        // (LD/ST)ELEM_U2
        public static ushort[]? Test_Array_U2;
        // (LD/ST)ELEM_U4
        public static uint[]? Test_Array_U4;
        // (LD/ST)ELEM_U8
        public static ulong[]? Test_Array_U8;

        // (LD/ST)ELEM_R4
        public static float[]? Test_Array_R4;
        // (LD/ST)ELEM_R8
        public static double[]? Test_Array_R8;

        // (LD/ST)ELEM_REF
        public static object[]? Test_Array_Ref;

        // (LD/ST)ELEM (with type token)
        public static DateTime[]? Test_Array_Struct;
    }
}
