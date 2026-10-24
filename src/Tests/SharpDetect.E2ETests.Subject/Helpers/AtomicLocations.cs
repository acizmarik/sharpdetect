// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject.Helpers;

public class AtomicLocations
{
    public static readonly int[] Test_Atomic_ArrayElement_Static = new int[1];
    public static readonly AtomicFlagHolder Test_Atomic_StructField_Holder = new();
    public static AtomicFlag Test_Atomic_StructField_Static;
}

public struct AtomicFlag
{
    public int Value;
}

public class AtomicFlagHolder
{
    public AtomicFlag Flag;
}

public enum AtomicFlagState
{
    Pending,
    Published
}

public static class GenericAtomicFlag<T>
{
    public static int Value;

    public static void Publish()
    {
        Interlocked.Exchange(ref Value, 1);
    }
}