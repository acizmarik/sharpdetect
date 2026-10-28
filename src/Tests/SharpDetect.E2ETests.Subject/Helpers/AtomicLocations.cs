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

public static class GenericVolatileFlag<T>
{
    public static volatile int Value;
}

public static class GenericAtomicFlag<T>
{
    public static int Value;

    public static void Publish()
    {
        Interlocked.Exchange(ref Value, 1);
    }

    public class AtomicReferenceSlot<TValue>
        where TValue : class
    {
        private TValue? _value;

        public TValue? Publish(TValue value)
        {
            return Interlocked.CompareExchange(ref _value, value, null);
        }
    }

#if NET9_0_OR_GREATER
    public class AtomicUnconstrainedSlot<TValue>
    {
        private TValue? _value;

        public TValue? Publish(TValue value)
        {
            return Interlocked.CompareExchange(ref _value, value, default);
        }
    }
#endif
}

public class AtomicNode
{
}

public class AtomicNode<T> : AtomicNode
{
}

public class AtomicNodeSlot<TNode>
    where TNode : AtomicNode
{
    private TNode? _value;

    public TNode? Publish(TNode value, TNode expected)
    {
        return Interlocked.CompareExchange(ref _value, value, expected);
    }
}

public class AtomicGenericNodeSlot<TNode>
    where TNode : AtomicNode<int>
{
    private TNode? _value;

    public TNode? Publish(TNode value, TNode expected)
    {
        return Interlocked.CompareExchange(ref _value, value, expected);
    }
}

public class AtomicExceptionSlot<TException>
    where TException : Exception
{
    private TException? _value;

    public TException? Publish(TException value, TException expected)
    {
        return Interlocked.CompareExchange(ref _value, value, expected);
    }
}
