// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject.Helpers.Fields
{
    public class GenericInstanceFieldOnReferenceType<T>
    {
        public T? ValueField;
        public T? ReferenceField;

        public void WriteValueField(T value) => ValueField = value;
        public T? ReadValueField() => ValueField;
        public void WriteReferenceField(T value) => ReferenceField = value;
        public T? ReadReferenceField() => ReferenceField;

        public void GenericMethodWrite<U>(U value) where U : T => ValueField = value;
        public T? GenericMethodRead<U>() => ValueField;
    }
}

