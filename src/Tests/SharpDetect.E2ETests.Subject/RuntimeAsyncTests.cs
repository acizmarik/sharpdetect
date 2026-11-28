// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;
using SharpDetect.E2ETests.Subject.Helpers.DataRaces;
using SharpDetect.E2ETests.Subject.Helpers.Fields;

namespace SharpDetect.E2ETests.Subject
{
    public static partial class Program
    {
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
