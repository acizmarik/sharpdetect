﻿namespace SharpDetect.E2ETests.Subject.Helpers.DataRaces
{
    public class DataRace
    {
        public static object? Test_DataRace_ReferenceType_Static;
        public static int Test_DataRace_ValueType_Static;
        public object? Test_DataRace_ReferenceType_Instance;
        public int Test_DataRace_ValueType_Instance;
    }
}
