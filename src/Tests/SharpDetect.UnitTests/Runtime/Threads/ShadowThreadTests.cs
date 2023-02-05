using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Common;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Runtime.Scheduling;
using SharpDetect.Core.Runtime.Threads;
using Xunit;

namespace SharpDetect.UnitTests.Runtime.Threads
{
    public class ShadowThreadTests : TestsBase
    {

        [Fact]
        public static void ShadowThread_Initialize()
        {
            // Prepare
            const int processId = 123;
            var threadId = new UIntPtr(456);
            const int virtualThreadId = 789;
            using var shadowThread = new ShadowThread(processId, threadId, virtualThreadId, new NullLoggerFactory(), new EpochSource());

            // Assert
            Assert.Equal(processId, shadowThread.ProcessId);
            Assert.Equal(threadId, shadowThread.Id);
            Assert.Equal($"{nameof(ShadowThread)}-{virtualThreadId}", shadowThread.DisplayName);
            Assert.Equal(0ul, shadowThread.Epoch.Value);
            Assert.Equal(0, shadowThread.GetCallstackDepth());
        }

        [Fact]
        public static void ShadowThread_SetName()
        {
            // Prepare
            const int processId = 123;
            var threadId = new UIntPtr(456);
            const int virtualThreadId = 789;
            const string threadName = "NewThreadName";
            using var shadowThread = new ShadowThread(processId, threadId, virtualThreadId, new NullLoggerFactory(), new EpochSource());

            // Act
            shadowThread.SetName(threadName);

            // Assert
            Assert.Equal(threadName, shadowThread.DisplayName);
        }

        [Fact]
        public static void ShadowThread_EnterFunction()
        {
            // Prepare
            const int processId = 123;
            var threadId = new UIntPtr(456);
            const int virtualThreadId = 789;
            using var shadowThread = new ShadowThread(processId, threadId, virtualThreadId, new NullLoggerFactory(), new EpochSource());
            var functionInfo = new FunctionInfo(new(123), new(456), new(789));
            var arguments = new[] { new object(), "Hello world" };

            // Act
            shadowThread.PushCallStack(functionInfo, MethodInterpretation.Regular, arguments);

            // Assert
            Assert.Equal(1, shadowThread.GetCallstackDepth());
            Assert.Equal(functionInfo, shadowThread.PeekCallstack().FunctionInfo);
            Assert.Equal(arguments, shadowThread.PeekCallstack().Arguments);
        }

        [Fact]
        public static void ShadowThread_ExitFunction()
        {
            // Prepare
            const int processId = 123;
            var threadId = new UIntPtr(456);
            const int virtualThreadId = 789;
            using var shadowThread = new ShadowThread(processId, threadId, virtualThreadId, new NullLoggerFactory(), new EpochSource());
            var functionInfo = new FunctionInfo(new(123), new(456), new(789));
            var arguments = new[] { new object(), "Hello world" };
            shadowThread.PushCallStack(functionInfo, MethodInterpretation.Regular, arguments);

            // Act
            shadowThread.PopCallStack();

            // Assert
            Assert.Equal(0, shadowThread.GetCallstackDepth());
        }

        [Fact]
        public static void ShadowThread_SimpleTasks()
        {
            // Prepare
            const int processId = 123;
            var threadId = new UIntPtr(456);
            const int virtualThreadId = 789;
            var task1Executed = false;
            var task2Executed = false;

            // Act
            using (var shadowThread = new ShadowThread(processId, threadId, virtualThreadId, new NullLoggerFactory(), new EpochSource()))
            {
                shadowThread.Start();
                shadowThread.Execute(notificationId: 0, flags: JobFlags.Concurrent, () =>
                {
                    task1Executed = true;
                });
                shadowThread.Execute(notificationId: 1, flags: JobFlags.Concurrent, () =>
                {
                    task2Executed = true;
                });
            }

            // Assert
            Assert.True(task1Executed);
            Assert.True(task2Executed);
        }

        [Fact]
        public static void ShadowThread_DoNotExecuteOutsideEpoch()
        {
            // Prepare
            const int processId = 123;
            var threadId = new UIntPtr(456);
            const int virtualThreadId = 789;
            var task1Executed = false;
            var task2Executed = false;

            // Act
            using (var shadowThread = new ShadowThread(processId, threadId, virtualThreadId, new NullLoggerFactory(), new EpochSource()))
            {
                shadowThread.Start();

                // This should be executed right-away
                shadowThread.Execute(notificationId: 0, flags: JobFlags.Concurrent, () =>
                {
                    task1Executed = true;
                    shadowThread.EnterNewEpoch();
                });

                // This should not be executed (shadow thread is waiting for next epoch)
                shadowThread.Execute(notificationId: 1, flags: JobFlags.Concurrent, () =>
                {
                    task2Executed = true;
                });
            }

            // Assert
            Assert.True(task1Executed);
            Assert.False(task2Executed);
        }

        [Fact]
        public static void ShadowThread_ExecuteWaitingJobsOnEpochChanged()
        {
            // Prepare
            const int processId = 123;
            var threadId = new UIntPtr(456);
            const int virtualThreadId = 789;
            var task1Executed = false;
            var task2Executed = false;
            var signaller = new EpochSource();

            // Act
            using (var shadowThread = new ShadowThread(processId, threadId, virtualThreadId, new NullLoggerFactory(), signaller))
            {
                shadowThread.Start();

                // This should be executed right-away
                shadowThread.Execute(notificationId: 0, flags: JobFlags.Concurrent, () =>
                {
                    task1Executed = true;
                    shadowThread.EnterNewEpoch();
                });

                // This should be executed after signallization of new global epoch
                shadowThread.Execute(notificationId: 1, flags: JobFlags.Concurrent, () =>
                {
                    task2Executed = true;
                });

                // Enter new global epoch
                signaller.Increment();
            }

            // Assert
            Assert.True(task1Executed);
            Assert.True(task2Executed);
        }
    }
}
