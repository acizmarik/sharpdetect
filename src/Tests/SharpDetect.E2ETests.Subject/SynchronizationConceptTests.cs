// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using SharpDetect.E2ETests.Subject.Helpers.DataRaces;

namespace SharpDetect.E2ETests.Subject;

public static partial class Program
{
    public static void Test_NoDataRace_ReaderWriterLockSlim_WriteThenRead()
    {
        using var rwLock = new ReaderWriterLockSlim();
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                rwLock.EnterWriteLock();
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
                rwLock.ExitWriteLock();
            },
            () =>
            {
                rwLock.EnterReadLock();
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
                rwLock.ExitReadLock();
            });
    }

    public static void Test_NoDataRace_ReaderWriterLockSlim_UpgradedWriteThenRead()
    {
        using var rwLock = new ReaderWriterLockSlim();
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                rwLock.EnterUpgradeableReadLock();
                rwLock.EnterWriteLock();
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
                rwLock.ExitWriteLock();
                rwLock.ExitUpgradeableReadLock();
            },
            () =>
            {
                if (!rwLock.TryEnterReadLock(WaitTimeout))
                    throw new TimeoutException("Could not enter the read lock.");
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
                rwLock.ExitReadLock();
            });
    }

    public static void Test_DataRace_ReaderWriterLockSlim_WritesUnderReadLock()
    {
        using var rwLock = new ReaderWriterLockSlim();
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                rwLock.EnterReadLock();
                instance.Test_DataRace_ValueType_InstanceProperty = 1;
                rwLock.ExitReadLock();
            },
            () =>
            {
                rwLock.EnterReadLock();
                instance.Test_DataRace_ValueType_InstanceProperty = 2;
                rwLock.ExitReadLock();
            });
    }

    public static void Test_NoDataRace_ManualResetEventSlim_SetThenWait()
    {
        using var signal = new ManualResetEventSlim();
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
                signal.Set();
            },
            () =>
            {
                SpinUntil(() => signal.IsSet);
                signal.Wait();
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_DataRace_ManualResetEventSlim_WriteAfterSet()
    {
        using var signal = new ManualResetEventSlim();
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                signal.Set();
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
            },
            () =>
            {
                signal.Wait();
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_NoDataRace_CountdownEvent_EverySignalBeforeWait()
    {
        using var countdown = new CountdownEvent(2);
        var first = new DataRace();
        var second = new DataRace();
        RunConcurrently(
            () =>
            {
                first.Test_DataRace_ValueType_InstanceProperty = 1;
                countdown.Signal();
            },
            () =>
            {
                second.Test_DataRace_ValueType_InstanceProperty = 2;
                countdown.Signal();
            },
            () =>
            {
                SpinUntil(() => countdown.IsSet);
                countdown.Wait();
                _ = first.Test_DataRace_ValueType_InstanceProperty;
                _ = second.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_DataRace_CountdownEvent_WriteAfterSignal()
    {
        using var countdown = new CountdownEvent(1);
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                countdown.Signal();
                instance.Test_DataRace_ValueType_InstanceProperty = 1;
            },
            () =>
            {
                countdown.Wait();
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_NoDataRace_Barrier_WriteBeforePhaseReadAfter()
    {
        using var barrier = new Barrier(2);
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
                barrier.SignalAndWait();
            },
            () =>
            {
                barrier.SignalAndWait();
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_DataRace_Barrier_WriteAfterPhase()
    {
        using var barrier = new Barrier(2);
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                barrier.SignalAndWait();
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
            },
            () =>
            {
                barrier.SignalAndWait();
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_NoDataRace_ConcurrentQueue_PublishThenRead()
    {
        var queue = new ConcurrentQueue<DataRace>();
        RunConcurrently(
            () => queue.Enqueue(new DataRace { Test_DataRace_ValueType_InstanceProperty = 42 }),
            () => _ = SpinUntilTaken<DataRace>(queue.TryDequeue).Test_DataRace_ValueType_InstanceProperty);
    }

    public static void Test_DataRace_ConcurrentQueue_PostPublicationWrite()
    {
        var queue = new ConcurrentQueue<DataRace>();
        RunConcurrently(
            () =>
            {
                var instance = new DataRace();
                queue.Enqueue(instance);
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
            },
            () => _ = SpinUntilTaken<DataRace>(queue.TryDequeue).Test_DataRace_ValueType_InstanceProperty);
    }

    public static void Test_NoDataRace_ConcurrentStack_PublishThenRead()
    {
        var stack = new ConcurrentStack<DataRace>();
        RunConcurrently(
            () => stack.Push(new DataRace { Test_DataRace_ValueType_InstanceProperty = 42 }),
            () => _ = SpinUntilTaken<DataRace>(stack.TryPop).Test_DataRace_ValueType_InstanceProperty);
    }

    public static void Test_NoDataRace_ConcurrentBag_PublishThenRead()
    {
        var bag = new ConcurrentBag<DataRace>();
        RunConcurrently(
            () => bag.Add(new DataRace { Test_DataRace_ValueType_InstanceProperty = 42 }),
            () => _ = SpinUntilTaken<DataRace>(bag.TryTake).Test_DataRace_ValueType_InstanceProperty);
    }

    public static void Test_NoDataRace_Channel_PublishThenRead()
    {
        var channel = Channel.CreateUnbounded<DataRace>();
        RunConcurrently(
            () => channel.Writer.TryWrite(new DataRace { Test_DataRace_ValueType_InstanceProperty = 42 }),
            () => _ = SpinUntilTaken<DataRace>(channel.Reader.TryRead).Test_DataRace_ValueType_InstanceProperty);
    }

    public static void Test_NoDataRace_ThreadPoolQueueUserWorkItem_PublishThenRead()
    {
        using var done = new ManualResetEventSlim();
        var instance = new DataRace { Test_DataRace_ValueType_InstanceProperty = 42 };
        ThreadPool.QueueUserWorkItem(_ =>
        {
            _ = instance.Test_DataRace_ValueType_InstanceProperty;
            done.Set();
        });

        if (!done.Wait(WaitTimeout))
            throw new TimeoutException("The work item did not run.");
    }

    public static void Test_DataRace_ThreadPoolQueueUserWorkItem_WriteAfterQueue()
    {
        using var done = new ManualResetEventSlim();
        var instance = new DataRace();
        ThreadPool.QueueUserWorkItem(_ =>
        {
            _ = instance.Test_DataRace_ValueType_InstanceProperty;
            done.Set();
        });

        instance.Test_DataRace_ValueType_InstanceProperty = 42;
        if (!done.Wait(WaitTimeout))
            throw new TimeoutException("The work item did not run.");
    }
    
    public static void Test_NoDataRace_Semaphore_FullReleaseDoesNotShiftPermits()
    {
        using var semaphore = new Semaphore(0, 1);
        var instance = new DataRace();
        semaphore.Release();
        try
        {
            semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
        }

        semaphore.WaitOne();
        RunConcurrently(
            () =>
            {
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
                semaphore.Release();
            },
            () =>
            {
                if (!semaphore.WaitOne(WaitTimeout))
                    throw new TimeoutException("The permit was never released.");
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_Deadlock_SimpleDeadlock_UsingReaderWriterLockSlimWriteLocks()
    {
        using var mutex = Mutex.OpenExisting(GetSynchronizationMutexName());
        var first = new ReaderWriterLockSlim();
        var second = new ReaderWriterLockSlim();
        var thread1HoldsFirst = new SemaphoreSlim(0, 1);
        var thread2HoldsSecond = new SemaphoreSlim(0, 1);

        var thread1 = new Thread(() =>
        {
            Thread.CurrentThread.Name = "TEST_Thread1";
            first.EnterWriteLock();
            thread1HoldsFirst.Release();
            thread2HoldsSecond.Wait();
            second.EnterWriteLock();
        });

        var thread2 = new Thread(() =>
        {
            Thread.CurrentThread.Name = "TEST_Thread2";
            second.EnterWriteLock();
            thread2HoldsSecond.Release();
            thread1HoldsFirst.Wait();
            first.EnterWriteLock();
        });

        thread1.IsBackground = true;
        thread2.IsBackground = true;
        thread1.Start();
        thread2.Start();
        mutex.WaitOne(WaitTimeout);
    }

    public static void Test_NoDeadlock_ReaderWriterLockSlimReadLocksInOppositeOrder()
    {
        using var first = new ReaderWriterLockSlim();
        using var second = new ReaderWriterLockSlim();
        using var barrier = new Barrier(2);
        RunConcurrently(
            () => AcquireBothReadLocks(first, second, barrier),
            () => AcquireBothReadLocks(second, first, barrier));
    }

    private static void AcquireBothReadLocks(ReaderWriterLockSlim outer, ReaderWriterLockSlim inner, Barrier barrier)
    {
        outer.EnterReadLock();
        barrier.SignalAndWait();
        inner.EnterReadLock();
        inner.ExitReadLock();
        outer.ExitReadLock();
    }

    private delegate bool TryTake<T>([MaybeNullWhen(false)] out T item);

    private static T SpinUntilTaken<T>(TryTake<T> tryTake)
    {
        T? item = default;
        SpinUntil(() => tryTake(out item));
        return item!;
    }

    private static void SpinUntil(Func<bool> condition)
    {
        if (!SpinWait.SpinUntil(condition, WaitTimeout))
            throw new TimeoutException("Timed out while spinning on a condition.");
    }
}
