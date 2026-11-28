// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#if NET11_0_OR_GREATER
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using SharpDetect.E2ETests.Subject.Helpers.DataRaces;
using SharpDetect.E2ETests.Subject.Helpers.Fields;

namespace SharpDetect.E2ETests.Subject
{
    public static partial class Program
    {
        public static void Test_RuntimeAsync_SuspendResume()
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = RuntimeAsyncSuspendOnceAsync(gate.Task);
            gate.SetResult();
            task.Wait();
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncSuspendOnceAsync(Task gate)
        {
            await gate.ConfigureAwait(false);
        }

        public static void Test_RuntimeAsync_SuspendResume_WithAsyncDebugger()
        {
            // What an attached debugger does: resumptions then go through InstrumentedDispatchContinuations
            typeof(Task)
                .GetField("s_asyncDebuggingEnabled", BindingFlags.NonPublic | BindingFlags.Static)!
                .SetValue(null, true);
            var activeFlags = typeof(object).Assembly
                .GetType("System.Runtime.CompilerServices.AsyncInstrumentation", throwOnError: true)!
                .GetField("s_activeFlags", BindingFlags.NonPublic | BindingFlags.Static)!;
            const uint synchronizeFlag = 0x80000000;
            activeFlags.SetValue(null, Enum.ToObject(activeFlags.FieldType, synchronizeFlag));

            Test_RuntimeAsync_SuspendResume();
        }

        public static void Test_RuntimeAsync_HookedTaskReturningMethods()
        {
            RuntimeAsyncAwaitHookedMethodsAsync().Wait();

            RuntimeAsyncHooked_TaskReturning(Task.Delay(10)).Wait();
            RuntimeAsyncHooked_RuntimeAsync(Task.Delay(10)).Wait();
            RuntimeAsyncHooked_GenericTaskReturning(new object()).Wait();
            RuntimeAsyncHooked_GenericTaskReturning(42).Wait();
        }

        // Runtime async callers call the runtime async variants of the awaited methods
        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncAwaitHookedMethodsAsync()
        {
            await RuntimeAsyncHooked_TaskReturning(Task.Delay(10));
            await RuntimeAsyncHooked_RuntimeAsync(Task.Delay(10));
            await RuntimeAsyncHooked_GenericTaskReturning(new object());
            await RuntimeAsyncHooked_GenericTaskReturning(42);
        }

        public static Task RuntimeAsyncHooked_TaskReturning(Task task)
        {
            return task;
        }

        [RuntimeAsyncMethodGeneration(true)]
        public static async Task RuntimeAsyncHooked_RuntimeAsync(Task task)
        {
            await task;
        }

        public static Task<T> RuntimeAsyncHooked_GenericTaskReturning<T>(T value)
        {
            return Task.Delay(10).ContinueWith(_ => value, TaskScheduler.Default);
        }

        public static void Test_NoDataRace_RuntimeAsync_WriteBeforeAwait_ReadAfterResume()
        {
            // The continuation runs inline on the completing thread, which has no other ordering with the suspended method
            var gate = new TaskCompletionSource();
            var mainThread = Thread.CurrentThread;
            var completer = new Thread(() =>
            {
                SpinUntilBlocked(mainThread);
                gate.SetResult();
            });
            completer.Start();

            RuntimeAsyncWriteBeforeAwaitReadAfterResumeAsync(gate.Task).Wait();
            completer.Join();
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncWriteBeforeAwaitReadAfterResumeAsync(Task gate)
        {
            DataRace.Test_RuntimeAsync_ValueType_Static = 42;
            await gate.ConfigureAwait(false);
            _ = DataRace.Test_RuntimeAsync_ValueType_Static;
        }

        public static void Test_NoDataRace_RuntimeAsync_WriteAfterFirstResume_ReadAfterSecondResume()
        {
            // Each resumption runs inline on its own completing thread, so the write and the read happen on different threads
            var firstGate = new TaskCompletionSource();
            var secondGate = new TaskCompletionSource();
            var mainThread = Thread.CurrentThread;
            var firstCompleter = new Thread(() =>
            {
                SpinUntilBlocked(mainThread);
                firstGate.SetResult();
            });
            // Unlike Join, IsAlive adds no ordering with the first completer
            var secondCompleter = new Thread(() =>
            {
                SpinUntilGateOpens(() => !firstCompleter.IsAlive);
                secondGate.SetResult();
            });
            firstCompleter.Start();
            secondCompleter.Start();

            RuntimeAsyncWriteAfterFirstResumeReadAfterSecondResumeAsync(firstGate.Task, secondGate.Task).Wait();
            firstCompleter.Join();
            secondCompleter.Join();
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncWriteAfterFirstResumeReadAfterSecondResumeAsync(Task firstGate, Task secondGate)
        {
            await firstGate.ConfigureAwait(false);
            DataRace.Test_RuntimeAsync_ValueType_Static = 42;
            await RuntimeAsyncAwaitAsync(secondGate);
            _ = DataRace.Test_RuntimeAsync_ValueType_Static;
        }

        // Awaited directly by a runtime async caller, so both frames suspend and resume together
        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncAwaitAsync(Task gate)
        {
            await gate.ConfigureAwait(false);
        }

        public static void Test_RuntimeAsync_CallerContinuesOnCapturedContext()
        {
            // The callee resumes inline on the completing thread and completes there. Its caller then continues on the
            // synchronization context captured on the main thread, so the frames are handed over without suspending again
            using var context = new DedicatedThreadSynchronizationContext();
            var gate = new TaskCompletionSource();
            var previousContext = SynchronizationContext.Current;
            Task task;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                task = RuntimeAsyncContinueOnCapturedContextAsync(gate.Task);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }

            var completer = new Thread(() => gate.SetResult());
            completer.Start();
            completer.Join();
            task.Wait();
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncContinueOnCapturedContextAsync(Task gate)
        {
            await RuntimeAsyncAwaitAsync(gate);
        }

        private sealed class DedicatedThreadSynchronizationContext : SynchronizationContext, IDisposable
        {
            private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
            private readonly Thread _thread;

            public DedicatedThreadSynchronizationContext()
            {
                _thread = new Thread(() =>
                {
                    SetSynchronizationContext(this);
                    foreach (var (callback, state) in _queue.GetConsumingEnumerable())
                        callback(state);
                });
                _thread.Start();
            }

            public override void Post(SendOrPostCallback d, object? state)
            {
                _queue.Add((d, state));
            }

            public void Dispose()
            {
                _queue.CompleteAdding();
                _thread.Join();
                _queue.Dispose();
            }
        }

        public static void Test_NoDataRace_RuntimeAsync_WriteAfterResume_ReadAfterJoin()
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = RuntimeAsyncWriteAfterResumeAsync(gate.Task);
            gate.SetResult();
            task.Wait();
            _ = DataRace.Test_RuntimeAsync_ValueType_Static;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncWriteAfterResumeAsync(Task gate)
        {
            await gate.ConfigureAwait(false);
            DataRace.Test_RuntimeAsync_ValueType_Static = 42;
        }

        public static void Test_DataRace_RuntimeAsync_ConcurrentResumes_WriteWriteRace()
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var bothResumed = new Barrier(participantCount: 2);
            var first = RuntimeAsyncWriteAfterResumeAsync(gate.Task, bothResumed, 1);
            var second = RuntimeAsyncWriteAfterResumeAsync(gate.Task, bothResumed, 2);
            gate.SetResult();
            Task.WaitAll(first, second);
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncWriteAfterResumeAsync(Task gate, Barrier bothResumed, int value)
        {
            await gate.ConfigureAwait(false);
            bothResumed.SignalAndWait(WaitTimeout);
            DataRace.Test_DataRace_ValueType_Static = value;
        }

        public static void Test_NoDataRace_SemaphoreSlimAsync_RuntimeAsyncAwaiters_ProtectedWriteRead()
        {
            var sem = new SemaphoreSlim(1, 1);
            var task1 = Task.Run([RuntimeAsyncMethodGeneration(true)] async () =>
            {
                await sem.WaitAsync();
                DataRace.Test_DataRace_ValueType_Static = 42;
                sem.Release();
            });
            var task2 = Task.Run([RuntimeAsyncMethodGeneration(true)] async () =>
            {
                await sem.WaitAsync();
                _ = DataRace.Test_DataRace_ValueType_Static;
                sem.Release();
            });
            Task.WaitAll(task1, task2);
        }

        public static void Test_NoDataRace_SemaphoreSlimAsync_RuntimeAsyncAwaiters_HighContention_WriteRead()
        {
            const int iterations = 1000;
            var sem = new SemaphoreSlim(1, 1);
            var writer = Task.Run([RuntimeAsyncMethodGeneration(true)] async () =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    await sem.WaitAsync();
                    DataRace.Test_DataRace_ValueType_Static = i;
                    sem.Release();
                }
            });
            var reader = Task.Run([RuntimeAsyncMethodGeneration(true)] async () =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    await sem.WaitAsync();
                    _ = DataRace.Test_DataRace_ValueType_Static;
                    sem.Release();
                }
            });
            Task.WaitAll(writer, reader);
        }

        public static void Test_NoDataRace_SemaphoreSlimAsync_RuntimeAsyncAndSynchronousWaiters_HighContention_WriteRead()
        {
            const int iterations = 1000;
            var sem = new SemaphoreSlim(1, 1);
            var writer = Task.Run([RuntimeAsyncMethodGeneration(true)] async () =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    await sem.WaitAsync();
                    DataRace.Test_DataRace_ValueType_Static = i;
                    sem.Release();
                }
            });
            var reader = new Thread(() =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    sem.Wait();
                    _ = DataRace.Test_DataRace_ValueType_Static;
                    sem.Release();
                }
            });
            reader.Start();
            writer.Wait();
            reader.Join();
        }

        public static void Test_Field_RuntimeAsync_Static_WriteRead()
        {
            RuntimeAsyncWriteAwaitReadAsync().Wait();
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncWriteAwaitReadAsync()
        {
            StaticFieldValueType.Test_Field_ValueType_Static = 42;
            await Task.Yield();
            _ = StaticFieldValueType.Test_Field_ValueType_Static;
        }

        public static void Test_Field_TaskReturningAwaitedByRuntimeAsync_Static_Write()
        {
            WriteStaticTaskReturning().Wait();
            RuntimeAsyncAwaitWriteStaticTaskReturningAsync().Wait();
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task RuntimeAsyncAwaitWriteStaticTaskReturningAsync()
        {
            await WriteStaticTaskReturning();
        }

        private static Task WriteStaticTaskReturning()
        {
            StaticFieldValueType.Test_Field_ValueType_Static = 42;
            return Task.CompletedTask;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class RuntimeAsyncMethodGenerationAttribute(bool runtimeAsync) : Attribute
    {
        public bool RuntimeAsync { get; } = runtimeAsync;
    }
}
#endif
