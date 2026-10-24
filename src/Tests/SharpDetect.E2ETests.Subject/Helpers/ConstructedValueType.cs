// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject.Helpers;

// newobj on a value type pushes the value itself, not an object reference
public struct ConstructedValueType(int value)
{
    public int ValueField = value;
}

public struct ConstructedValueType<T>(T value)
{
    public T ValueField = value;
}