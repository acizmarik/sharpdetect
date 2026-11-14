// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using SharpDetect.E2ETests.Subject.Helpers.DataRaces;

namespace SharpDetect.E2ETests.Subject;

public static partial class Program
{
    private static readonly TimeSpan ScheduleSteeringDelay = TimeSpan.FromMilliseconds(300);

    public static void Test_DataRace_ArrayElement_WriteWriteRace()
    {
        var elements = new int[4];
        RunConcurrently(
            () => elements[0] = 1,
            () => elements[0] = 2);
    }

    public static void Test_DataRace_EmbeddedStructField_WriteWriteRace()
    {
        var holder = new EmbeddedStructHolder();
        RunConcurrently(
            () => holder.Embedded.Value = 1,
            () => holder.Embedded.Value = 2);
    }

    public static void Test_DataRace_RefParameter_WriteWriteRace()
    {
        var instance = new DataRace();
        RunConcurrently(
            () => WriteThroughReference(ref instance.Test_DataRace_ValueType_InstanceField, 1),
            () => WriteThroughReference(ref instance.Test_DataRace_ValueType_InstanceField, 2));
    }

    public static void Test_DataRace_ConstructorEscape_WriteAfterFirstFieldAndThreadStart()
    {
        var instance = new SelfPublishingInstance(initializeBeforeStart: true);
        if (!instance.Reader.Join(WaitTimeout))
            throw new TimeoutException("The reader thread did not finish.");
    }

    public static void Test_DataRace_ConstructorEscape_WriteAfterThreadStart()
    {
        var instance = new SelfPublishingInstance();
        if (!instance.Reader.Join(WaitTimeout))
            throw new TimeoutException("The reader thread did not finish.");
    }

    public static void Test_DataRace_LoopConditionalStore_WriteWriteRace()
    {
        var instance = new DataRace();
        RunConcurrently(
            () => StoreConditionalValueInLoop(instance),
            () => StoreConditionalValueInLoop(instance));
    }

    public static void Test_DataRace_AsCastReceiver_WriteWriteRace()
    {
        object instance = new DataRace();
        RunConcurrently(
            () => (instance as DataRace)!.Test_DataRace_ValueType_InstanceField = 1,
            () => (instance as DataRace)!.Test_DataRace_ValueType_InstanceField = 2);
    }

    public static void Test_DataRace_Task_UnjoinedTasksOnOnePoolThread()
    {
        Task.Run(() => DataRace.Test_DataRace_ValueType_Static = 1);
        Thread.Sleep(ScheduleSteeringDelay);
        Task.Run(() => DataRace.Test_DataRace_ValueType_Static = 2);
        Thread.Sleep(ScheduleSteeringDelay);
    }

    public static void Test_NoDataRace_Task_WaitAll_ReadAfterWaitAll()
    {
        var first = new DataRace();
        var second = new DataRace();
        var firstTask = RunOnDedicatedThread(() => first.Test_DataRace_ValueType_InstanceProperty = 1);
        var secondTask = RunOnDedicatedThread(() =>
        {
            Thread.Sleep(ScheduleSteeringDelay);
            second.Test_DataRace_ValueType_InstanceProperty = 2;
        });

        if (!Task.WaitAll([firstTask, secondTask], WaitTimeout))
            throw new TimeoutException("The tasks did not finish.");

        _ = first.Test_DataRace_ValueType_InstanceProperty;
        _ = second.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_NoDataRace_Task_WaitAll_TasksAlreadyCompleted()
    {
        var first = new DataRace();
        var second = new DataRace();
        var firstTask = RunOnDedicatedThread(() => first.Test_DataRace_ValueType_InstanceProperty = 1);
        var secondTask = RunOnDedicatedThread(() => second.Test_DataRace_ValueType_InstanceProperty = 2);

        Thread.Sleep(ScheduleSteeringDelay);
        Task.WaitAll(firstTask, secondTask);

        _ = first.Test_DataRace_ValueType_InstanceProperty;
        _ = second.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_NoDataRace_Task_WhenAll_TasksAlreadyCompleted()
    {
        var first = new DataRace();
        var second = new DataRace();
        var firstTask = RunOnDedicatedThread(() => first.Test_DataRace_ValueType_InstanceProperty = 1);
        var secondTask = RunOnDedicatedThread(() => second.Test_DataRace_ValueType_InstanceProperty = 2);

        Thread.Sleep(ScheduleSteeringDelay);
        if (!Task.WhenAll(firstTask, secondTask).Wait(WaitTimeout))
            throw new TimeoutException("The tasks did not finish.");

        _ = first.Test_DataRace_ValueType_InstanceProperty;
        _ = second.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_NoDataRace_Task_WhenAll_ReadInContinuation()
    {
        var first = new DataRace();
        var firstTask = RunOnDedicatedThread(() => first.Test_DataRace_ValueType_InstanceProperty = 1);
        var secondTask = RunOnDedicatedThread(() => Thread.Sleep(ScheduleSteeringDelay));

        var continuation = Task.WhenAll(firstTask, secondTask)
            .ContinueWith(_ => Consume(first.Test_DataRace_ValueType_InstanceProperty), TaskContinuationOptions.ExecuteSynchronously);
        if (!SpinWait.SpinUntil(() => continuation.IsCompleted, WaitTimeout))
            throw new TimeoutException("The continuation did not run.");
    }

    public static void Test_NoDataRace_TaskFactory_ContinueWhenAll_ReadInContinuation()
    {
        var first = new DataRace();
        var firstTask = RunOnDedicatedThread(() => first.Test_DataRace_ValueType_InstanceProperty = 1);
        var secondTask = RunOnDedicatedThread(() => Thread.Sleep(ScheduleSteeringDelay));

        var continuation = Task.Factory.ContinueWhenAll(
            [firstTask, secondTask],
            _ => Consume(first.Test_DataRace_ValueType_InstanceProperty),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        if (!SpinWait.SpinUntil(() => continuation.IsCompleted, WaitTimeout))
            throw new TimeoutException("The continuation did not run.");
    }

    public static void Test_NoDataRace_Task_WhenAllOfResults_ReadAfterWait()
    {
        var first = new DataRace();
        var firstTask = Task.Factory.StartNew(
            () => first.Test_DataRace_ValueType_InstanceProperty = 1,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        var secondTask = Task.Factory.StartNew(
            () =>
            {
                Thread.Sleep(ScheduleSteeringDelay);
                return 2;
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        if (!Task.WhenAll(firstTask, secondTask).Wait(WaitTimeout))
            throw new TimeoutException("The tasks did not finish.");

        _ = first.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_DataRace_Task_WaitAll_UnawaitedTaskStaysUnordered()
    {
        var awaited = new DataRace();
        var unawaited = new DataRace();
        var awaitedTask = RunOnDedicatedThread(() => awaited.Test_DataRace_ValueType_InstanceProperty = 1);
        var unawaitedTask = RunOnDedicatedThread(() => unawaited.Test_DataRace_ValueType_InstanceProperty = 2);

        Thread.Sleep(ScheduleSteeringDelay);
        Task.WaitAll(awaitedTask);

        _ = unawaited.Test_DataRace_ValueType_InstanceProperty;
        GC.KeepAlive(unawaitedTask);
    }

    public static void Test_NoDataRace_Task_WhenAll_ReadAfterAwait()
    {
        if (!ReadAfterWhenAllAsync().Wait(WaitTimeout))
            throw new TimeoutException("The tasks did not finish.");
    }

    public static void Test_NoDataRace_ParallelFor_ReadAfterLoop()
    {
        var instances = Enumerable.Range(0, 8).Select(_ => new DataRace()).ToArray();
        Parallel.For(0, instances.Length, i =>
        {
            instances[i].Test_DataRace_ValueType_InstanceProperty = i;
            Thread.Sleep(20);
        });

        foreach (var instance in instances)
            _ = instance.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_NoDataRace_ParallelFor_BodyThrows()
    {
        var instances = Enumerable.Range(0, 8).Select(_ => new DataRace()).ToArray();
        try
        {
            Parallel.For(0, instances.Length, i =>
            {
                instances[i].Test_DataRace_ValueType_InstanceProperty = i;
                throw new InvalidOperationException("The loop body fails.");
            });
        }
        catch (AggregateException)
        {
            // Every body that started has finished by the time the loop rethrows
        }

        foreach (var instance in instances)
            _ = instance.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_NoDataRace_ParallelForEach_ReadAfterLoop()
    {
        var instances = Enumerable.Range(0, 8).Select(_ => new DataRace()).ToArray();
        Parallel.ForEach(instances.Where(_ => true), instance =>
        {
            instance.Test_DataRace_ValueType_InstanceProperty = 1;
            Thread.Sleep(20);
        });

        foreach (var instance in instances)
            _ = instance.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_NoDataRace_ParallelInvoke_ReadAfterInvoke()
    {
        var first = new DataRace();
        var second = new DataRace();
        // The first action runs on the calling thread, so the pooled one has finished before the tasks are awaited
        Parallel.Invoke(
            () =>
            {
                Thread.Sleep(ScheduleSteeringDelay);
                first.Test_DataRace_ValueType_InstanceProperty = 1;
            },
            () => second.Test_DataRace_ValueType_InstanceProperty = 2);

        _ = first.Test_DataRace_ValueType_InstanceProperty;
        _ = second.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_NoDataRace_LockOnTypeObject_ProtectedWriteRead()
    {
        var instance = new DataRace();
        RunConcurrently(
            () => IncrementUnderTypeLock(instance),
            () => IncrementUnderTypeLock(instance));
    }

    public static void Test_NoDataRace_LockOnTypeObject_AcrossGarbageCollection()
    {
        // The lock object lives outside the garbage-collected heap, like every Type object and string literal
        var instance = new DataRace();
        RunConcurrently(
            () => IncrementUnderTypeLock(instance),
            () =>
            {
                Thread.Sleep(ScheduleSteeringDelay);
                GC.Collect();
                IncrementUnderTypeLock(instance);
            });
    }

    public static void Test_NoDataRace_SynchronizedMethod_ProtectedWriteRead()
    {
        var counter = new SynchronizedCounter();
        RunConcurrently(counter.Increment, counter.Increment);
    }

    public static void Test_NoDataRace_SpinLock_ProtectedWriteRead()
    {
        var spinLock = new SpinLock(enableThreadOwnerTracking: false);
        var instance = new DataRace();
        RunConcurrently(
            () => IncrementUnderSpinLock(ref spinLock, instance),
            () => IncrementUnderSpinLock(ref spinLock, instance));
    }

    public static void Test_NoDataRace_ConcurrentQueueOfValueType_PublishThenRead()
    {
        var queue = new ConcurrentQueue<int>();
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
                queue.Enqueue(1);
            },
            () =>
            {
                if (!SpinWait.SpinUntil(() => queue.TryDequeue(out _), WaitTimeout))
                    throw new TimeoutException("Nothing was enqueued.");
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_NoDataRace_CancellationToken_CancelThenObserve()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
                cancellation.Cancel();
            },
            () =>
            {
                if (!SpinWait.SpinUntil(() => token.IsCancellationRequested, WaitTimeout))
                    throw new TimeoutException("Cancellation was not requested.");
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    public static void Test_DataRace_CancellationToken_WriteAfterCancel()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var instance = new DataRace();
        RunConcurrently(
            () =>
            {
                cancellation.Cancel();
                instance.Test_DataRace_ValueType_InstanceProperty = 42;
            },
            () =>
            {
                if (!SpinWait.SpinUntil(() => token.IsCancellationRequested, WaitTimeout))
                    throw new TimeoutException("Cancellation was not requested.");
                Thread.Sleep(ScheduleSteeringDelay);
                _ = instance.Test_DataRace_ValueType_InstanceProperty;
            });
    }

    private static async Task ReadAfterWhenAllAsync()
    {
        var first = new DataRace();
        var second = new DataRace();
        var firstTask = RunOnDedicatedThread(() => first.Test_DataRace_ValueType_InstanceProperty = 1);
        var secondTask = RunOnDedicatedThread(() =>
        {
            Thread.Sleep(ScheduleSteeringDelay);
            second.Test_DataRace_ValueType_InstanceProperty = 2;
        });

        await Task.WhenAll(firstTask, secondTask);

        _ = first.Test_DataRace_ValueType_InstanceProperty;
        _ = second.Test_DataRace_ValueType_InstanceProperty;
    }

    private static Task RunOnDedicatedThread(Action action)
        => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private static void IncrementUnderSpinLock(ref SpinLock spinLock, DataRace instance)
    {
        for (var i = 0; i < 20; i++)
        {
            var lockTaken = false;
            spinLock.Enter(ref lockTaken);
            instance.Test_DataRace_ValueType_InstanceProperty++;
            spinLock.Exit();
        }
    }

    private static void StoreConditionalValueInLoop(DataRace instance)
    {
        for (var i = 0; i < 10; i++)
            instance.Test_DataRace_ValueType_InstanceField = i % 2 == 0 ? 1 : 2;
    }

    private static void IncrementUnderTypeLock(DataRace instance)
    {
        lock (typeof(SynchronizedCounter))
            instance.Test_DataRace_ValueType_InstanceProperty++;
    }

    private static void WriteThroughReference(ref int location, int value)
        => location = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(int value)
    {
    }

    private sealed class SynchronizedCounter
    {
        public int Count;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Increment() => Count++;
    }

    private struct EmbeddedStruct
    {
        public int Value;
    }

    private sealed class EmbeddedStructHolder
    {
        public EmbeddedStruct Embedded;
    }

    private sealed class SelfPublishingInstance
    {
        public readonly Thread Reader;
        public int State;
        public int Initialized;

        public SelfPublishingInstance(bool initializeBeforeStart = false)
        {
            if (initializeBeforeStart)
                Initialized = 1;

            // `this` escapes to the reader before the field below is initialized
            Reader = new Thread(ReadState);
            Reader.Start();
            State = 42;
        }

        private void ReadState()
        {
            Thread.Sleep(300);
            Consume(State);
        }
    }
}
