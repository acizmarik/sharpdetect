// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject.Helpers.Fields
{
    public class StaticFieldReferenceType
    {
        public static object? Test_Field_ReferenceType_Static;
        public static object? Test_Property_ReferenceType_Static { get; set; }
    }
}