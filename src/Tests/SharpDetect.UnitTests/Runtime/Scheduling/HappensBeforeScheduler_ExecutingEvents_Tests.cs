using dnlib.DotNet;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Common;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Runtime;
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
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 4, 0, 0, 0 }));

            // Act
            var methodCalledRaised = false;
            eventsHub.MethodCalled += _ => methodCalledRaised = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, functionMDToken), argslist, new RawEventInfo(2, pid, threadId));
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
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 4, 0, 0, 0 }));

            // Act
            var methodCalledRaised = false;
            var methodReturnedRaised = false;
            eventsHub.MethodCalled += _ => methodCalledRaised = true;
            eventsHub.MethodReturned += _ => methodReturnedRaised = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, functionMDToken), argslist, new RawEventInfo(2, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, functionMDToken), null, null, new RawEventInfo(3, pid, threadId));
            mainThread.Execute(4, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodCalledRaised);
            Assert.True(methodReturnedRaised);
        }

        [Fact(Skip = "NotImplementedYet")]
        public Task Scheduller_ExecutingEvent_FieldAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "NotImplementedYet")]
        public Task Scheduller_ExecutingEvent_FieldInstanceAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "NotImplementedYet")]
        public Task Scheduller_ExecutingEvent_ArrayElementAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "NotImplementedYet")]
        public Task Scheduller_ExecutingEvent_ArrayInstanceAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "NotImplementedYet")]
        public Task Scheduller_ExecutingEvent_ArrayIndexAccessed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Scheduller_ExecutingEvent_LockAcquireAttempted()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = typeof(Monitor).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var functionMDToken = new MDToken(typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Enter) && m.GetParameters().Length == 1)!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 8, 0, 0, 0 }));

            // Act
            var methodCalledRaised = false;
            var methodLockAcquireAttemptedCalledRaised = false;
            eventsHub.MethodCalled += _ => methodCalledRaised = true;
            eventsHub.LockAcquireAttempted += _ => methodLockAcquireAttemptedCalledRaised = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, functionMDToken), argslist, new RawEventInfo(3, pid, threadId));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodLockAcquireAttemptedCalledRaised);
            Assert.True(methodCalledRaised);
        }

        [Fact]
        public async Task Scheduller_ExecutingEvent_LockAcquireReturned()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = typeof(Monitor).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var functionMDToken = new MDToken(typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Enter) && m.GetParameters().Length == 1)!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 8, 0, 0, 0 }));

            // Act
            var methodCalledRaised = false;
            var methodLockAcquireAttemptedCalledRaised = false;
            var methodLockAcquireReturnedCalledRaised = false;
            var methodReturnedRaised = false;
            eventsHub.MethodCalled += _ => methodCalledRaised = true;
            eventsHub.LockAcquireAttempted += _ => methodLockAcquireAttemptedCalledRaised = true;
            eventsHub.LockAcquireReturned += _ => methodLockAcquireReturnedCalledRaised = true;
            eventsHub.MethodReturned += _ => methodReturnedRaised = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, functionMDToken), argslist, new RawEventInfo(3, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, functionMDToken), null, null, new RawEventInfo(5, pid, threadId));
            mainThread.Execute(6, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodLockAcquireAttemptedCalledRaised);
            Assert.True(methodCalledRaised);
            Assert.True(methodLockAcquireReturnedCalledRaised);
            Assert.True(methodReturnedRaised);
        }

        [Fact]
        public async Task Scheduller_ExecutingEvent_LockReleaseCalled()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = typeof(Monitor).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var functionMDToken = new MDToken(typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Enter) && m.GetParameters().Length == 1)!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 8, 0, 0, 0 }));

            // Act
            var methodCalledRaised = false;
            var methodReturnedRaised = false;
            var methodLockAcquireAttemptedCalledRaised = false;
            var methodLockAcquireReturnedCalledRaised = false;
            eventsHub.MethodCalled += _ => methodCalledRaised = true;
            eventsHub.MethodReturned += _ => methodReturnedRaised = true;
            eventsHub.LockAcquireAttempted += _ => methodLockAcquireAttemptedCalledRaised = true;
            eventsHub.LockAcquireReturned += _ => methodLockAcquireReturnedCalledRaised = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, functionMDToken), argslist, new RawEventInfo(3, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, functionMDToken), null, argslist, new RawEventInfo(5, pid, threadId));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodLockAcquireAttemptedCalledRaised);
            Assert.True(methodCalledRaised);
            Assert.True(methodReturnedRaised);
            Assert.True(methodLockAcquireReturnedCalledRaised);
        }

        [Fact]
        public async Task Scheduller_ExecutingEvent_LockReleaseReturned()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = typeof(Monitor).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var acquireFunctionMDToken = new MDToken(typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Enter) && m.GetParameters().Length == 1)!.MetadataToken);
            var releaseFunctionMDToken = new MDToken(typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Exit))!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 8, 0, 0, 0 }));

            // Act
            var methodLockAcquireAttemptedCalledRaised = false;
            var methodLockAcquireReturnedCalledRaised = false;
            var methodLockReleaseCalledRaised = false;
            var methodLockReleaseReturnedCalledRaised = false;
            eventsHub.LockAcquireAttempted += _ => methodLockAcquireAttemptedCalledRaised = true;
            eventsHub.LockAcquireReturned += _ => methodLockAcquireReturnedCalledRaised = true;
            eventsHub.LockReleaseCalled += _ => methodLockReleaseCalledRaised = true;
            eventsHub.LockReleaseReturned += _ => methodLockReleaseReturnedCalledRaised = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            // Lock acquire
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, acquireFunctionMDToken), argslist, new RawEventInfo(3, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, acquireFunctionMDToken), null, null, new RawEventInfo(5, pid, threadId));
            // Lock release
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, releaseFunctionMDToken),argslist, new RawEventInfo(7, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, releaseFunctionMDToken), null, null, new RawEventInfo(9, pid, threadId));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodLockAcquireAttemptedCalledRaised);
            Assert.True(methodLockAcquireReturnedCalledRaised);
            Assert.True(methodLockReleaseCalledRaised);
            Assert.True(methodLockReleaseReturnedCalledRaised);
        }

        [Fact]
        public async Task Scheduller_ExecutingEvent_ObjectWaitAttempted()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = typeof(Monitor).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var acquireFunctionMDToken = new MDToken(typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Enter) && m.GetParameters().Length == 1)!.MetadataToken);
            var waitFunctionMDToken = new MDToken(typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Wait) && m.GetParameters().Length == 1)!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 8, 0, 0, 0 }));

            // Act
            var methodLockAcquireAttemptedCalledRaised = false;
            var methodLockAcquireReturnedRaised = false;
            var methodObjectWaitAttemptedCalledRaised = false;
            eventsHub.LockAcquireAttempted += _ => methodLockAcquireAttemptedCalledRaised = true;
            eventsHub.LockAcquireReturned += _ => methodLockAcquireReturnedRaised = true;
            eventsHub.ObjectWaitAttempted += _ => methodObjectWaitAttemptedCalledRaised = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            // Acquire lock
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, acquireFunctionMDToken), argslist, new RawEventInfo(3, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, acquireFunctionMDToken), null, null, new RawEventInfo(5, pid, threadId));
            // Object wait
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, waitFunctionMDToken), argslist, new RawEventInfo(7, pid, threadId));
            mainThread.Execute(8, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodLockAcquireAttemptedCalledRaised);
            Assert.True(methodLockAcquireReturnedRaised);
            Assert.True(methodObjectWaitAttemptedCalledRaised);
        }

        [Fact]
        public async Task Scheduller_ExecutingEvent_ObjectWaitReturned()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = typeof(Monitor).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var acquireFunctionMDToken = new MDToken(typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Enter) && m.GetParameters().Length == 1)!.MetadataToken);
            var waitFunctionMDToken = new MDToken(typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Wait) && m.GetParameters().Length == 1)!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 8, 0, 0, 0 }));
            var returnValue = new RawReturnValue(ByteString.CopyFrom(new byte[] { 1 }));

            // Act
            var methodLockAcquireAttemptedCalledRaised = false;
            var methodLockAcquireReturnedRaised = false;
            var methodObjectWaitAttemptedCalledRaised = false;
            var methodObjectWaitReturnedRaised = false;
            eventsHub.LockAcquireAttempted += _ => methodLockAcquireAttemptedCalledRaised = true;
            eventsHub.LockAcquireReturned += _ => methodLockAcquireReturnedRaised = true;
            eventsHub.ObjectWaitAttempted += _ => methodObjectWaitAttemptedCalledRaised = true;
            eventsHub.ObjectWaitReturned += _ => methodObjectWaitReturnedRaised = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            // Acquire lock
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, acquireFunctionMDToken), argslist, new RawEventInfo(3, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, acquireFunctionMDToken), null, null, new RawEventInfo(5, pid, threadId));
            // Object wait
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, waitFunctionMDToken), argslist, new RawEventInfo(7, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, waitFunctionMDToken), returnValue, null, new RawEventInfo(9, pid, threadId));
            mainThread.Execute(10, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodLockAcquireAttemptedCalledRaised);
            Assert.True(methodLockAcquireReturnedRaised);
            Assert.True(methodObjectWaitAttemptedCalledRaised);
            Assert.True(methodObjectWaitReturnedRaised);
        }

        [Fact]
        public async Task Scheduller_ExecutingEvent_ObjectPulseCalled()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = typeof(Monitor).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var functionMDToken = new MDToken(typeof(Monitor).GetMethod(nameof(Monitor.Pulse))!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 8, 0, 0, 0 }));

            // Act
            var methodCalledRaised = false;
            var methodObjectPulseCalledRaised = false;
            eventsHub.MethodCalled += _ => methodCalledRaised = true;
            eventsHub.ObjectPulseCalled += _ => methodObjectPulseCalledRaised = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, functionMDToken), argslist, new RawEventInfo(3, pid, threadId));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodObjectPulseCalledRaised);
            Assert.True(methodCalledRaised);
        }

        [Fact]
        public async Task Scheduller_ExecutingEvent_ObjectPulseReturned()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = typeof(Monitor).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var functionMDToken = new MDToken(typeof(Monitor).GetMethod(nameof(Monitor.Pulse))!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, profilingClient, new UtcDateTimeProvider(), new NullLoggerFactory());
            var executionCompletion = new TaskCompletionSource();
            var argslist = new RawArgumentsList(ByteString.CopyFrom(new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 }), ByteString.CopyFrom(new byte[] { 8, 0, 0, 0 }));

            // Act
            var methodCalledRaised = false;
            var methodReturnedRaised = false;
            var methodObjectPulseCalledReturned = false;
            eventsHub.MethodCalled += _ => methodCalledRaised = true;
            eventsHub.MethodReturned += _ => methodReturnedRaised = true;
            eventsHub.ObjectPulseReturned += _ => methodObjectPulseCalledReturned = true;
            scheduler.Schedule_ProfilerInitialized(default(Version), new RawEventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new RawEventInfo(1, pid, threadId));
            scheduler.Schedule_MethodCalled(new(moduleId, typeMDToken, functionMDToken), argslist, new RawEventInfo(3, pid, threadId));
            scheduler.Schedule_MethodReturned(new(moduleId, typeMDToken, functionMDToken), null, null, new RawEventInfo(4, pid, threadId));
            mainThread.Execute(4, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodCalledRaised);
            Assert.True(methodObjectPulseCalledReturned);
            Assert.True(methodReturnedRaised);
        }

        internal static void TestMethod(int value) { }
    }
}
