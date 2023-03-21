namespace SharpDetect.E2ETests.Subject
{
    public static partial class Program
    {
        public static void Verify_FieldElementAccess_Instrumentation()
        {
            Test_Field_ValueType_Instance_Read();
            Test_Field_ValueType_Instance_Write();
            Test_Field_ReferenceType_Instance_Read();
            Test_Field_ReferenceType_Instance_Write();
            Test_Field_ValueType_Static_Read();
            Test_Field_ValueType_Static_Write();
            Test_Field_ReferenceType_Static_Read();
            Test_Field_ReferenceType_Static_Write();
            Test_Property_ValueType_Instance_Read();
            Test_Property_ValueType_Instance_Write();
            Test_Property_ReferenceType_Instance_Read();
            Test_Property_ReferenceType_Instance_Write();
            Test_Property_ValueType_Static_Read();
            Test_Property_ValueType_Static_Write();
            Test_Property_ReferenceType_Static_Read();
            Test_Property_ReferenceType_Static_Write();
        }

        public static void Verify_ArrayElementAccess_Instrumentation()
        {
            Test_Array_I_Read();
            Test_Array_I_Write();
            Test_Array_I1_Read();
            Test_Array_I1_Write();
            Test_Array_I2_Read();
            Test_Array_I2_Write();
            Test_Array_I4_Read();
            Test_Array_I4_Write();
            Test_Array_I8_Read();
            Test_Array_I8_Write();
            Test_Array_U1_Read();
            Test_Array_U1_Write();
            Test_Array_U2_Read();
            Test_Array_U2_Write();
            Test_Array_U4_Read();
            Test_Array_U4_Write();
            Test_Array_U8_Read();
            Test_Array_U8_Write();
            Test_Array_R4_Read();
            Test_Array_R4_Write();
            Test_Array_R8_Read();
            Test_Array_R8_Write();
            Test_Array_Ref_Read();
            Test_Array_Ref_Write();
            Test_Array_Struct_Read();
            Test_Array_Struct_Write();
        }
    }
}
