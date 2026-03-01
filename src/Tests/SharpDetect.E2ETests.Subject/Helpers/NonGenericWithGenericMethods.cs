// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject.Helpers.Fields
{
    public class NonGenericWithGenericMethods
    {
        public int ValueField;
        public object? ReferenceField;

        public void GenericMethodWriteValue<T>(T value) where T : struct
        {
            ValueField = (int)(object)value;
        }

        public T GenericMethodReadValue<T>() where T : struct
        {
            return (T)(object)ValueField;
        }

        public void GenericMethodWriteReference<T>(T value) where T : class
        {
            ReferenceField = value;
        }

        public T? GenericMethodReadReference<T>() where T : class
        {
            return ReferenceField as T;
        }
    }
}

