// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject.Helpers.Fields
{
    public class ComplexTypeInstanceFieldOnReferenceType<T>
    {
        public T[]? ArrayField;
        public List<T>? ListField;
    }
}
