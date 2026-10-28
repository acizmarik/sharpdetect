// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject.Helpers;

public class GenericSelfReferencedField<T>
{
    public static T? StaticValue;
    public T? Value;

    public static T? ReadStatic() => StaticValue;
    public static void WriteStatic(T? value) => StaticValue = value;
    public T? Read() => Value;
}