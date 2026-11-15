// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using SharpDetect.E2ETests.Subject.Helpers.DataRaces;

namespace SharpDetect.E2ETests.Subject;

public static partial class Program
{
    private const int GateTimeoutMilliseconds = 5000;

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
        var first = Task.Run(() => DataRace.Test_DataRace_ValueType_Static = 1);
        var second = Task.Run(() => DataRace.Test_DataRace_ValueType_Static = 2);
        SpinUntilCompleted(first, second);
    }

    public static void Test_NoDataRace_Task_WaitAll_ReadAfterWaitAll()
    {
        var first = new DataRace();
        var second = new DataRace();
        var caller = Thread.CurrentThread;
        var firstTask = RunOnDedicatedThread(() => first.Test_DataRace_ValueType_InstanceProperty = 1);
        var secondTask = RunOnDedicatedThread(() =>
        {
            // Finish only once the caller blocks inside WaitAll
            SpinUntilBlocked(caller);
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

        SpinUntilCompleted(firstTask, secondTask);
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

        SpinUntilCompleted(firstTask, secondTask);
        if (!Task.WhenAll(firstTask, secondTask).Wait(WaitTimeout))
            throw new TimeoutException("The tasks did not finish.");

        _ = first.Test_DataRace_ValueType_InstanceProperty;
        _ = second.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_NoDataRace_Task_WhenAll_ReadInContinuation()
    {
        using var continuationRegistered = new ManualResetEventSlim();
        var first = new DataRace();
        var firstTask = RunOnDedicatedThread(() =>
        {
            WaitForSignal(continuationRegistered);
            first.Test_DataRace_ValueType_InstanceProperty = 1;
        });
        // Finish last, so the continuation runs on this thread
        var secondTask = RunOnDedicatedThread(() => SpinUntilCompleted(firstTask));

        var continuation = Task.WhenAll(firstTask, secondTask)
            .ContinueWith(_ => Consume(first.Test_DataRace_ValueType_InstanceProperty), TaskContinuationOptions.ExecuteSynchronously);
        continuationRegistered.Set();
        SpinUntilCompleted(continuation);
    }

    public static void Test_NoDataRace_TaskFactory_ContinueWhenAll_ReadInContinuation()
    {
        using var continuationRegistered = new ManualResetEventSlim();
        var first = new DataRace();
        var firstTask = RunOnDedicatedThread(() =>
        {
            WaitForSignal(continuationRegistered);
            first.Test_DataRace_ValueType_InstanceProperty = 1;
        });
        // Finish last, so the continuation runs on this thread
        var secondTask = RunOnDedicatedThread(() => SpinUntilCompleted(firstTask));

        var continuation = Task.Factory.ContinueWhenAll(
            [firstTask, secondTask],
            _ => Consume(first.Test_DataRace_ValueType_InstanceProperty),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        continuationRegistered.Set();
        SpinUntilCompleted(continuation);
    }

    public static void Test_NoDataRace_Task_WhenAllOfResults_ReadAfterWait()
    {
        using var whenAllCreated = new ManualResetEventSlim();
        var first = new DataRace();
        var firstTask = Task.Factory.StartNew(
            () =>
            {
                WaitForSignal(whenAllCreated);
                return first.Test_DataRace_ValueType_InstanceProperty = 1;
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        // Finish last, so this thread completes the promise
        var secondTask = Task.Factory.StartNew(
            () =>
            {
                SpinUntilCompleted(firstTask);
                return 2;
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        var whenAll = Task.WhenAll(firstTask, secondTask);
        whenAllCreated.Set();
        if (!whenAll.Wait(WaitTimeout))
            throw new TimeoutException("The tasks did not finish.");

        _ = first.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_DataRace_Task_WaitAll_UnawaitedTaskStaysUnordered()
    {
        var awaited = new DataRace();
        var unawaited = new DataRace();
        var awaitedTask = RunOnDedicatedThread(() => awaited.Test_DataRace_ValueType_InstanceProperty = 1);
        var unawaitedTask = RunOnDedicatedThread(() => unawaited.Test_DataRace_ValueType_InstanceProperty = 2);

        SpinUntilCompleted(awaitedTask, unawaitedTask);
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
        var interleaving = new ParallelLoopInterleaving();
        var instances = Enumerable.Range(0, 8).Select(_ => new DataRace()).ToArray();
        Parallel.For(0, instances.Length, interleaving.Options, i =>
        {
            interleaving.EnterBody();
            instances[i].Test_DataRace_ValueType_InstanceProperty = i;
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
        var interleaving = new ParallelLoopInterleaving();
        var instances = Enumerable.Range(0, 8).Select(_ => new DataRace()).ToArray();
        Parallel.ForEach(instances.Where(_ => true), interleaving.Options, instance =>
        {
            interleaving.EnterBody();
            instance.Test_DataRace_ValueType_InstanceProperty = 1;
        });

        foreach (var instance in instances)
            _ = instance.Test_DataRace_ValueType_InstanceProperty;
    }

    public static void Test_NoDataRace_ParallelInvoke_ReadAfterInvoke()
    {
        using var pooledActionStarted = new ManualResetEventSlim();
        var first = new DataRace();
        var second = new DataRace();
        // The first action runs on the calling thread; waiting for the pooled one to start keeps WaitAll from inlining it
        Parallel.Invoke(
            () =>
            {
                WaitForSignal(pooledActionStarted);
                first.Test_DataRace_ValueType_InstanceProperty = 1;
            },
            () =>
            {
                pooledActionStarted.Set();
                second.Test_DataRace_ValueType_InstanceProperty = 2;
            });

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
        var firstTask = RunOnDedicatedThread(() => IncrementUnderTypeLock(instance));
        var secondTask = RunOnDedicatedThread(() =>
        {
            SpinUntilCompleted(firstTask);
            GC.Collect();
            IncrementUnderTypeLock(instance);
        });

        if (!Task.WaitAll([firstTask, secondTask], WaitTimeout))
            throw new TimeoutException("The tasks did not finish.");
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
        var writer = RunOnDedicatedThread(() =>
        {
            cancellation.Cancel();
            instance.Test_DataRace_ValueType_InstanceProperty = 42;
        });
        var reader = RunOnDedicatedThread(() =>
        {
            SpinUntilGateOpens(() => token.IsCancellationRequested);
            SpinUntilCompleted(writer);
            _ = instance.Test_DataRace_ValueType_InstanceProperty;
        });

        if (!Task.WaitAll([writer, reader], WaitTimeout))
            throw new TimeoutException("The tasks did not finish.");
    }

    private static async Task ReadAfterWhenAllAsync()
    {
        var first = new DataRace();
        var second = new DataRace();
        var caller = Thread.CurrentThread;
        var firstTask = RunOnDedicatedThread(() =>
        {
            SpinUntilBlocked(caller);
            first.Test_DataRace_ValueType_InstanceProperty = 1;
        });
        var secondTask = RunOnDedicatedThread(() =>
        {
            SpinUntilCompleted(firstTask);
            second.Test_DataRace_ValueType_InstanceProperty = 2;
        });

        await Task.WhenAll(firstTask, secondTask);

        _ = first.Test_DataRace_ValueType_InstanceProperty;
        _ = second.Test_DataRace_ValueType_InstanceProperty;
    }

    private static Task RunOnDedicatedThread(Action action)
        => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private static void SpinUntilCompleted(params Task[] tasks)
        => SpinUntilGateOpens(() => tasks.All(task => task.IsCompleted));

    private static void SpinUntilBlocked(Thread thread)
        => SpinUntilGateOpens(() => (thread.ThreadState & ThreadState.WaitSleepJoin) != 0);

    private static void SpinUntilGateOpens(Func<bool> condition)
    {
        if (!SpinWait.SpinUntil(condition, GateTimeoutMilliseconds))
            throw new TimeoutException("Timed out while spinning on a condition.");
    }

    private static void WaitForSignal(ManualResetEventSlim signal)
    {
        if (!signal.Wait(GateTimeoutMilliseconds))
            throw new TimeoutException("The signal was not set.");
    }

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

    private sealed class ParallelLoopInterleaving : TaskScheduler
    {
        private readonly Thread _caller = Thread.CurrentThread;
        private readonly ManualResetEventSlim _callerTookItem = new();
        private Thread? _worker;
        private bool _callerHeldItem;

        public ParallelOptions Options => new() { MaxDegreeOfParallelism = 2, TaskScheduler = this };

        public void EnterBody()
        {
            if (Thread.CurrentThread != _caller)
            {
                WaitForSignal(_callerTookItem);
                return;
            }

            if (_callerHeldItem)
                return;

            _callerHeldItem = true;
            _callerTookItem.Set();
            SpinUntilGateOpens(() => _worker is { IsAlive: false });
        }

        protected override void QueueTask(Task task)
        {
            var thread = new Thread(() => TryExecuteTask(task));
            _worker ??= thread;
            thread.Start();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            => TryExecuteTask(task);

        protected override IEnumerable<Task> GetScheduledTasks()
            => [];
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
            Reader.Start(Thread.CurrentThread);
            State = 42;
        }

        private void ReadState(object? creator)
        {
            SpinUntilBlocked((Thread)creator!);
            Consume(State);
        }
    }
}
