// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.E2ETests.Subject.Helpers;
using SharpDetect.E2ETests.Subject.Helpers.DataRaces;
using SharpDetect.E2ETests.Subject.Helpers.Fields;

namespace SharpDetect.E2ETests.Subject;

public static partial class Program
{
    public static void Test_NoDataRace_InterlockedFlag_PublishInValueArgument()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        DataRace.Test_Atomic_ValueType_Static = 0;
        RunConcurrently(
            () => Interlocked.Exchange(ref DataRace.Test_Atomic_ValueType_Static, WriteGuardedAndReturnFlag()),
            () =>
            {
                AcquireInterlockedFlag();
                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

    public static void Test_NoDataRace_InterlockedAcquire_ObservesPublishDuringArgumentEvaluation()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        DataRace.Test_Atomic_ValueType_Static = 0;
        var publisher = new Thread(() =>
        {
            DataRace.Test_DataRace_ValueType_Static = 42;
            Interlocked.Exchange(ref DataRace.Test_Atomic_ValueType_Static, 1);
        });

        // The publisher runs while the addend is computed, so the add observes the published flag
        if (Interlocked.Add(ref DataRace.Test_Atomic_ValueType_Static, StartAndAwaitPublisher(publisher)) == 1)
            _ = DataRace.Test_DataRace_ValueType_Static;

        publisher.Join();
    }

    public static void Test_NoDataRace_VolatileWrite_PublishInValueArgument()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        DataRace.Test_Atomic_ValueType_Static = 0;
        RunConcurrently(
            () => Volatile.Write(ref DataRace.Test_Atomic_ValueType_Static, WriteGuardedAndReturnFlag()),
            () =>
            {
                var spinner = new SpinWait();
                while (Volatile.Read(ref DataRace.Test_Atomic_ValueType_Static) == 0)
                    spinner.SpinOnce();

                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

        public static void Test_NoDataRace_InterlockedReference_PublishConstructedObject()
        {
            DataRace.Test_Atomic_PublishedObject_Static = null;
            RunConcurrently(
                () => Interlocked.CompareExchange(
                    ref DataRace.Test_Atomic_PublishedObject_Static,
                    new DataRace { Test_DataRace_ValueType_InstanceProperty = 42 },
                    null),
                () =>
                {
                    var spinner = new SpinWait();
                    DataRace? published;
                    while ((published = Interlocked.CompareExchange(ref DataRace.Test_Atomic_PublishedObject_Static, null, null)) is null)
                        spinner.SpinOnce();

                _ = published.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_DataRace_InterlockedFailedCompareExchange_DoesNotPublish()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        DataRace.Test_Atomic_ValueType_Static = 0;
        DataRace.Test_Atomic_ValueType_Static_Done = 0;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                // The flag is 0, so this exchange fails and stores nothing another thread could observe
                Interlocked.CompareExchange(ref DataRace.Test_Atomic_ValueType_Static, 2, 1);
                Interlocked.Exchange(ref DataRace.Test_Atomic_ValueType_Static_Done, 1);
            },
            () =>
            {
                // Acquires the flag only after the failed exchange completed
                var spinner = new SpinWait();
                while (DataRace.Test_Atomic_ValueType_Static_Done == 0)
                    spinner.SpinOnce();

                _ = Interlocked.CompareExchange(ref DataRace.Test_Atomic_ValueType_Static, 0, 0);
                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

    public static void Test_DataRace_InterlockedCounter_MixedWithPlainIncrement()
    {
        DataRace.Test_Atomic_ValueType_Static = 0;
        RunConcurrently(
            () => DataRace.Test_Atomic_ValueType_Static++,
            () => Interlocked.Increment(ref DataRace.Test_Atomic_ValueType_Static));
    }

    public static void Test_DataRace_InterlockedCounter_AtomicIncrementBeforePlainIncrement()
    {
        DataRace.Test_Atomic_ValueType_Static = 0;
        RunConcurrently(
            () => Interlocked.Increment(ref DataRace.Test_Atomic_ValueType_Static),
            () =>
            {
                var spinner = new SpinWait();
                while (DataRace.Test_Atomic_ValueType_Static == 0)
                    spinner.SpinOnce();

                DataRace.Test_Atomic_ValueType_Static++;
            });
    }

    public static void Test_DataRace_VolatileFlag_PlainWriteBeforeVolatileRead()
    {
        DataRace.Test_Atomic_ValueType_Static = 0;
        RunConcurrently(
            () => DataRace.Test_Atomic_ValueType_Static = 1,
            () =>
            {
                // The plain write releases nothing, so the read that observes it is not ordered after it
                var spinner = new SpinWait();
                while (Volatile.Read(ref DataRace.Test_Atomic_ValueType_Static) == 0)
                    spinner.SpinOnce();
            });
    }

    public static void Test_InterlockedCounter_PlainReadAfterAtomicIncrement()
    {
        DataRace.Test_Atomic_ValueType_Static = 0;
        RunConcurrently(
            () => Interlocked.Increment(ref DataRace.Test_Atomic_ValueType_Static),
            () =>
            {
                var spinner = new SpinWait();
                while (DataRace.Test_Atomic_ValueType_Static == 0)
                    spinner.SpinOnce();
            });
    }

    public static void Test_NoDataRace_InterlockedCounter_PlainAccessAfterJoin()
    {
        DataRace.Test_Atomic_ValueType_Static = 0;
        RunConcurrently(
            () => Interlocked.Increment(ref DataRace.Test_Atomic_ValueType_Static),
            () => Interlocked.Increment(ref DataRace.Test_Atomic_ValueType_Static));

        // Every increment happens-before the joins, so the plain accesses that follow do not race
        DataRace.Test_Atomic_ValueType_Static = DataRace.Test_Atomic_ValueType_Static * 2;
    }

    public static void Test_NoDataRace_InterlockedArrayElementFlag_PublishThenRead()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                Interlocked.Exchange(ref AtomicLocations.Test_Atomic_ArrayElement_Static[0], 1);
            },
            () =>
            {
                var spinner = new SpinWait();
                while (Interlocked.CompareExchange(ref AtomicLocations.Test_Atomic_ArrayElement_Static[0], 0, 0) == 0)
                    spinner.SpinOnce();

                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

    public static void Test_NoDataRace_InterlockedFlagThroughRefParameter_PublishThenRead()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        DataRace.Test_Atomic_ValueType_Static = 0;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                PublishFlagThroughRefParameter(ref DataRace.Test_Atomic_ValueType_Static);
            },
            () =>
            {
                AcquireInterlockedFlag();
                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

    public static void Test_NoDataRace_InterlockedStructFieldFlag_PublishThenRead()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                Interlocked.Exchange(ref AtomicLocations.Test_Atomic_StructField_Holder.Flag.Value, 1);
            },
            () =>
            {
                var spinner = new SpinWait();
                while (Interlocked.CompareExchange(ref AtomicLocations.Test_Atomic_StructField_Holder.Flag.Value, 0, 0) == 0)
                    spinner.SpinOnce();

                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

    public static void Test_NoDataRace_InterlockedStaticStructFieldFlag_PublishThenRead()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                Interlocked.Exchange(ref AtomicLocations.Test_Atomic_StructField_Static.Value, 1);
            },
            () =>
            {
                var spinner = new SpinWait();
                while (Interlocked.CompareExchange(ref AtomicLocations.Test_Atomic_StructField_Static.Value, 0, 0) == 0)
                    spinner.SpinOnce();

                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

    public static void Test_NoDataRace_InterlockedGenericStaticFlag_PublishThenRead()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        GenericStaticField<int>.Value = 0;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                Interlocked.Exchange(ref GenericStaticField<int>.Value, 1);
            },
            () =>
            {
                var spinner = new SpinWait();
                while (Interlocked.CompareExchange(ref GenericStaticField<int>.Value, 0, 0) == 0)
                    spinner.SpinOnce();

                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

    public static void Test_DataRace_InterlockedGenericStaticFlag_OtherInstantiationDoesNotPublish()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        GenericStaticField<int>.Value = 0;
        GenericStaticField<long>.Value = 0;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                Interlocked.Exchange(ref GenericStaticField<long>.Value, 1);
            },
            () =>
            {
                // Acquires the other instantiation only after the publish
                var spinner = new SpinWait();
                while (GenericStaticField<long>.Value == 0)
                    spinner.SpinOnce();

                _ = Interlocked.CompareExchange(ref GenericStaticField<int>.Value, 0, 0);
                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

    public static void Test_NoDataRace_InterlockedGenericFlagPublishedInsideGenericType_PublishThenRead()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
        GenericAtomicFlag<int>.Value = 0;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                // Inside the generic type the field is referenced as GenericAtomicFlag<T>.Value
                GenericAtomicFlag<int>.Publish();
            },
            () =>
            {
                var spinner = new SpinWait();
                while (Interlocked.CompareExchange(ref GenericAtomicFlag<int>.Value, 0, 0) == 0)
                    spinner.SpinOnce();

                _ = DataRace.Test_DataRace_ValueType_Static;
            });
    }

    public static void Test_NoDataRace_InterlockedByteFlag_PublishThenRead()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
#if NET9_0_OR_GREATER
        DataRace.Test_Atomic_Byte_Static = 0;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                Interlocked.Exchange(ref DataRace.Test_Atomic_Byte_Static, (byte)1);
            },
            () =>
            {
                var spinner = new SpinWait();
                while (Interlocked.CompareExchange(ref DataRace.Test_Atomic_Byte_Static, (byte)0, (byte)0) == 0)
                    spinner.SpinOnce();

                _ = DataRace.Test_DataRace_ValueType_Static;
            });
#endif
    }

    public static void Test_NoDataRace_InterlockedEnumFlag_PublishThenRead()
    {
        DataRace.Test_DataRace_ValueType_Static = 0;
#if NET9_0_OR_GREATER
        DataRace.Test_Atomic_Enum_Static = AtomicFlagState.Pending;
        RunConcurrently(
            () =>
            {
                DataRace.Test_DataRace_ValueType_Static = 42;
                Interlocked.Exchange(ref DataRace.Test_Atomic_Enum_Static, AtomicFlagState.Published);
            },
            () =>
            {
                var spinner = new SpinWait();
                while (Interlocked.CompareExchange(ref DataRace.Test_Atomic_Enum_Static, AtomicFlagState.Pending, AtomicFlagState.Pending)
                       == AtomicFlagState.Pending)
                {
                    spinner.SpinOnce();
                }

                _ = DataRace.Test_DataRace_ValueType_Static;
            });
#endif
    }

    public static void Test_NoDataRace_LazyInitializer_PublishThenRead()
    {
        DataRace.Test_LazyInitializer_ReferenceType_Static = null;
        RunConcurrently(
            ReadThroughLazyInitializer,
            ReadThroughLazyInitializer);
    }

    public static void Test_Field_Atomic_ExchangeReleasesAfterValueArgument()
    {
        Interlocked.Exchange(ref StaticFieldValueType.Test_Field_Atomic_ValueType_Static, WriteOrderingValueAndReturnOne());
    }

    public static void Test_Field_Atomic_CompareExchangeFails()
    {
        Interlocked.CompareExchange(ref StaticFieldValueType.Test_Field_Atomic_ValueType_Static, 1, 42);
    }

    public static void Test_Field_Atomic_CompareExchangeComparandThroughBranch()
    {
        StaticFieldReferenceType.Test_Field_Atomic_ReferenceType_Static = "published";
        StaticFieldReferenceType.Test_Field_Atomic_ReferenceType_Static_Comparand = StaticFieldReferenceType.Test_Field_Atomic_ReferenceType_Static;
        StaticFieldReferenceType.Test_Field_Atomic_ReferenceType_Static_Fallback = null;
        Interlocked.CompareExchange(
            ref StaticFieldReferenceType.Test_Field_Atomic_ReferenceType_Static,
            "replaced",
            StaticFieldReferenceType.Test_Field_Atomic_ReferenceType_Static_Comparand ?? StaticFieldReferenceType.Test_Field_Atomic_ReferenceType_Static_Fallback);
    }

    public static void Test_Field_Atomic_CompareExchangeOfGenericParameter()
    {
        new GenericAtomicFlag<int>.AtomicReferenceSlot<string>().Publish("published");
    }

    public static void Test_Field_Atomic_CompareExchangeOfBaseClassConstrainedParameter()
    {
        new AtomicNodeSlot<AtomicNode>().Publish(new AtomicNode(), new AtomicNode());
    }

    public static void Test_Field_Atomic_CompareExchangeOfGenericBaseClassConstrainedParameter()
    {
        new AtomicGenericNodeSlot<AtomicNode<int>>().Publish(new AtomicNode<int>(), new AtomicNode<int>());
    }

    public static void Test_Field_Atomic_CompareExchangeOfForeignBaseClassConstrainedParameter()
    {
        new AtomicExceptionSlot<InvalidOperationException>().Publish(new InvalidOperationException(), new InvalidOperationException());
    }

    public static void Test_Field_Atomic_CompareExchangeOfDoubleNaN()
    {
        StaticFieldValueType.Test_Field_Atomic_ValueType_Static_Double = double.NaN;
        Interlocked.CompareExchange(ref StaticFieldValueType.Test_Field_Atomic_ValueType_Static_Double, 1.0, double.NaN);
    }

    public static void Test_Field_Atomic_CompareExchangeOfDoubleNegativeZero()
    {
        StaticFieldValueType.Test_Field_Atomic_ValueType_Static_Double = -0.0;
        Interlocked.CompareExchange(ref StaticFieldValueType.Test_Field_Atomic_ValueType_Static_Double, 1.0, 0.0);
    }

    public static void Test_Field_Atomic_CompareExchangeOfSingleNaN()
    {
        StaticFieldValueType.Test_Field_Atomic_ValueType_Static_Single = float.NaN;
        Interlocked.CompareExchange(ref StaticFieldValueType.Test_Field_Atomic_ValueType_Static_Single, 1.0f, float.NaN);
    }

    public static void Test_Field_Atomic_CompareExchangeOfStruct()
    {
#if NET9_0_OR_GREATER
        try
        {
            Interlocked.CompareExchange(ref StaticFieldValueType.Test_Field_Atomic_ValueType_Static_Struct, default, default);
        }
        catch (NotSupportedException)
        {
            // The runtime only exchanges references, primitives and enums
        }
#endif
    }

    public static void Test_Field_Atomic_CompareExchangeOfUnconstrainedGenericParameter()
    {
#if NET9_0_OR_GREATER
        new GenericAtomicFlag<int>.AtomicUnconstrainedSlot<int>().Publish(1);
#endif
    }

    public static void Test_Field_Atomic_CompareExchangeOfEnum()
    {
#if NET9_0_OR_GREATER
        Interlocked.CompareExchange(ref StaticFieldValueType.Test_Field_Atomic_ValueType_Static_Enum, AtomicFlagState.Published, AtomicFlagState.Pending);
#endif
    }

    public static void Test_Field_Atomic_CompareExchangeOfByte()
    {
#if NET9_0_OR_GREATER
        Interlocked.CompareExchange(ref StaticFieldValueType.Test_Field_Atomic_ValueType_Static_Byte, (byte)1, (byte)0);
#endif
    }

    private static int WriteGuardedAndReturnFlag()
    {
        DataRace.Test_DataRace_ValueType_Static = 42;
        return 1;
    }

    private static int WriteOrderingValueAndReturnOne()
    {
        DataRace.Test_DataRace_ValueType_Static = 1;
        return 1;
    }

    private static int StartAndAwaitPublisher(Thread publisher)
    {
        publisher.Start();
        var spinner = new SpinWait();
        while (DataRace.Test_Atomic_ValueType_Static == 0)
            spinner.SpinOnce();

        return 0;
    }

    private static void PublishFlagThroughRefParameter(ref int flag)
    {
        Interlocked.Exchange(ref flag, 1);
    }

    private static void ReadThroughLazyInitializer()
    {
        var value = LazyInitializer.EnsureInitialized(
            ref DataRace.Test_LazyInitializer_ReferenceType_Static,
            () => new DataRace { Test_DataRace_ValueType_InstanceProperty = 42 });
        _ = value.Test_DataRace_ValueType_InstanceProperty;
    }
}