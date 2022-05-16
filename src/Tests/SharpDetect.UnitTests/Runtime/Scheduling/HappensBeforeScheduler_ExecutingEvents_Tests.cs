using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Runtime.Arguments;
using SharpDetect.Core.Runtime.Scheduling;
using SharpDetect.Core.Runtime.Threads;
using Xunit;

namespace SharpDetect.UnitTests.Runtime.Scheduling
{
    public class HappensBeforeScheduler_ExecutingEvents_Tests : TestsBase
    {
        [Fact]
        public async Task Scheduller_ExecutingEvent_MethodCalled()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var typeMDToken = new MDToken(typeof(HappensBeforeScheduler_ExecutingEvents_Tests).MetadataToken);
            var functionMDToken = new MDToken(typeof(HappensBeforeScheduler_ExecutingEvents_Tests).GetMethod(nameof(TestMethod), 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = ModuleBindContext;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, ModuleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var methodCalledRaised = false;
            eventsHub.MethodCalled += _ => methodCalledRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new EventInfo(1, pid, threadId));
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, functionMDToken), new (ushort, IValueOrPointer)[] { (0, new ValueOrPointer(123)) }, new EventInfo(2, pid, threadId));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodCalledRaised);
        }

        [Fact]
        public async Task Scheduller_ExecutingEvent_MethodReturned()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var typeMDToken = new MDToken(typeof(HappensBeforeScheduler_ExecutingEvents_Tests).MetadataToken);
            var functionMDToken = new MDToken(typeof(HappensBeforeScheduler_ExecutingEvents_Tests).GetMethod(nameof(TestMethod),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = ModuleBindContext;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, ModuleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var methodCalledRaised = false;
            var methodReturnedRaised = false;
            eventsHub.MethodCalled += _ => methodCalledRaised = true;
            eventsHub.MethodReturned += _ => methodReturnedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new EventInfo(1, pid, threadId));
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, functionMDToken), new (ushort, IValueOrPointer)[] { (0, new ValueOrPointer(123)) }, new EventInfo(2, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, functionMDToken), null, null, new EventInfo(3, pid, threadId));
            mainThread.Execute(4, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodCalledRaised);
            Assert.True(methodReturnedRaised);
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_FieldAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_FieldInstanceAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_ArrayElementAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_ArrayInstanceAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_ArrayIndexAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_LockAcquireAttempted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_LockAcquireReturned()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_LockReleaseCalled()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_LockReleaseReturned()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_ObjectWaitAttempted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_ObjectWaitReturned()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_ObjectPulseCalled()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public Task Scheduller_ExecutingEvent_ObjectPulseReturned()
        {
            throw new NotImplementedException();
        }

        internal static void TestMethod(int value) { }
    }
}
