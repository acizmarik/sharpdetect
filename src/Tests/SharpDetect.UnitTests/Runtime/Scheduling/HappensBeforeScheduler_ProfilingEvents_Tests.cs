using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.Services;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Runtime.Scheduling;
using SharpDetect.Core.Runtime.Threads;
using Xunit;

namespace SharpDetect.UnitTests.Runtime.Scheduling
{
    public class HappensBeforeScheduler_ProfilingEvents_Tests : TestsBase
    {
        [Fact]
        public async Task Scheduller_ProfilingEvent_ProfilerInitializedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(321);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            mainThread.Execute(1, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.Single(shadowCLR.Threads);
            Assert.Equal(mainThread, shadowCLR.Threads.First().Value);
            Assert.Equal(pid, shadowCLR.ProcessId);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(profilerInitializedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_ProfilerDestroyedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(321);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();
            scheduler.ProcessFinished += () => executionFinished.SetResult();

            // Act
            var profilerInitializedRaised = false;
            var profilerDestroyedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.ProfilerDestroyed += _ => profilerDestroyedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ProfilerDestroyed(new EventInfo(1, pid, threadId));
            await executionFinished.Task;

            // Assert
            Assert.Equal(ShadowRuntimeState.Terminated, shadowCLR.State);
            Assert.True(profilerInitializedRaised);
            Assert.True(profilerDestroyedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_ModuleLoadedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var moduleLoadedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.ModuleLoaded += _ => moduleLoadedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new EventInfo(1, pid, threadId));
            mainThread.Execute(2, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.Single(shadowCLR.Modules);
            Assert.Equal(moduleId, shadowCLR.Modules.First().Key.Id);
            Assert.Equal(modulePath, shadowCLR.Modules.First().Value.Location);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(profilerInitializedRaised);
            Assert.True(moduleLoadedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_TypeLoadedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var typeMDToken = new MDToken(typeof(HappensBeforeScheduler_ProfilingEvents_Tests).MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var moduleLoadedRaised = false;
            var typeLoadedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.ModuleLoaded += _ => moduleLoadedRaised = true;
            eventsHub.TypeLoaded += _ => typeLoadedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new EventInfo(1, pid, threadId));
            scheduler.Schedule_TypeLoaded(new TypeInfo(moduleId, typeMDToken), new EventInfo(2, pid, threadId));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.Single(shadowCLR.Modules);
            Assert.Single(shadowCLR.Types);
            Assert.Equal(moduleId, shadowCLR.Types.First().Key.ModuleId);
            Assert.Equal(typeMDToken, shadowCLR.Types.First().Key.TypeToken);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(profilerInitializedRaised);
            Assert.True(moduleLoadedRaised);
            Assert.True(typeLoadedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_JITCompilationStartedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var typeMDToken = new MDToken(typeof(HappensBeforeScheduler_ProfilingEvents_Tests).MetadataToken);
            var functionMDToken = new MDToken(typeof(HappensBeforeScheduler_ProfilingEvents_Tests).GetMethod(nameof(Scheduller_ProfilingEvent_JITCompilationStartedAsync))!.MetadataToken);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var moduleLoadedRaised = false;
            var typeLoadedRaised = false;
            var jitCompilationRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.ModuleLoaded += _ => moduleLoadedRaised = true;
            eventsHub.TypeLoaded += _ => typeLoadedRaised = true;
            eventsHub.JITCompilationStarted += _ => jitCompilationRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new EventInfo(1, pid, threadId));
            scheduler.Schedule_TypeLoaded(new TypeInfo(moduleId, typeMDToken), new EventInfo(2, pid, threadId));
            scheduler.Schedule_JITCompilationStarted(new FunctionInfo(moduleId, typeMDToken, functionMDToken), new EventInfo(3, pid, threadId));
            mainThread.Execute(4, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.Single(shadowCLR.Modules);
            Assert.Single(shadowCLR.Types);
            Assert.Single(shadowCLR.Functions);
            Assert.Equal(moduleId, shadowCLR.Types.First().Key.ModuleId);
            Assert.Equal(typeMDToken, shadowCLR.Types.First().Key.TypeToken);
            Assert.Equal(functionMDToken, shadowCLR.Functions.First().Key.FunctionToken);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(profilerInitializedRaised);
            Assert.True(moduleLoadedRaised);
            Assert.True(typeLoadedRaised);
            Assert.True(jitCompilationRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_ThreadCreatedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId1 = new(456);
            UIntPtr threadId2 = new(789);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var threadCreatedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.ThreadCreated += _ => threadCreatedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId1));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ThreadCreated(threadId2, new EventInfo(1, pid, threadId1));
            mainThread.Execute(2, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Equal(2, shadowCLR.Threads.Count);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId1, out var shadowThread1) && mainThread == shadowThread1);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId2, out var shadowThread2));
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(profilerInitializedRaised);
            Assert.True(threadCreatedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_ThreadDestroyedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId1 = new(456);
            UIntPtr threadId2 = new(789);
            ShadowThread mainThread;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var threadCreatedRaised = false;
            var threadDestroyedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.ThreadCreated += _ => threadCreatedRaised = true;
            eventsHub.ThreadDestroyed += _ => threadDestroyedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId1));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ThreadCreated(threadId2, new EventInfo(1, pid, threadId1));
            scheduler.Schedule_ThreadDestroyed(threadId2, new EventInfo(2, pid, threadId1));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Single(shadowCLR.Threads);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId1, out var shadowThread1) && mainThread == shadowThread1);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(profilerInitializedRaised);
            Assert.True(threadCreatedRaised);
            Assert.True(threadDestroyedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_RuntimeSuspendStartedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId1 = new(456);
            UIntPtr threadId2 = new(789);
            ShadowThread mainThread;
            var reason = COR_PRF_SUSPEND_REASON.GC;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var runtimeSuspendStartedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.RuntimeSuspendStarted += _ => runtimeSuspendStartedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId1));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_RuntimeSuspendStarted(reason, new EventInfo(1, pid, threadId1));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Single(shadowCLR.Threads);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId1, out var shadowThread1) && mainThread == shadowThread1);
            Assert.Equal(ShadowRuntimeState.Suspended, shadowCLR.State);
            Assert.True(shadowCLR.SuspensionReason.HasValue);
            Assert.Equal(reason, shadowCLR.SuspensionReason!.Value);
            Assert.True(profilerInitializedRaised);
            Assert.True(runtimeSuspendStartedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_RuntimeSuspendFinishedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId1 = new(456);
            UIntPtr threadId2 = new(789);
            ShadowThread mainThread;
            var reason = COR_PRF_SUSPEND_REASON.GC;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var runtimeSuspendStartedRaised = false;
            var runtimeSuspendFinishedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.RuntimeSuspendStarted += _ => runtimeSuspendStartedRaised = true;
            eventsHub.RuntimeSuspendFinished += _ => runtimeSuspendFinishedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId1));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_RuntimeSuspendStarted(reason, new EventInfo(1, pid, threadId1));
            scheduler.Schedule_RuntimeSuspendFinished(new EventInfo(2, pid, threadId1));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Single(shadowCLR.Threads);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId1, out var shadowThread1) && mainThread == shadowThread1);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.False(shadowCLR.SuspensionReason.HasValue);
            Assert.True(profilerInitializedRaised);
            Assert.True(runtimeSuspendStartedRaised);
            Assert.True(runtimeSuspendFinishedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_RuntimeThreadSuspendedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId1 = new(456);
            UIntPtr threadId2 = new(789);
            ShadowThread mainThread;
            var reason = COR_PRF_SUSPEND_REASON.GC;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var threadCreatedRaised = false;
            var runtimeSuspendStartedRaised = false;
            var runtimeThreadSuspendedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.ThreadCreated += _ => threadCreatedRaised = true;
            eventsHub.RuntimeSuspendStarted += _ => runtimeSuspendStartedRaised = true;
            eventsHub.RuntimeThreadSuspended += _ => runtimeThreadSuspendedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId1));
            scheduler.Schedule_ThreadCreated(threadId2, new EventInfo(1, pid, threadId1));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_RuntimeSuspendStarted(reason, new EventInfo(2, pid, threadId1));
            scheduler.Schedule_RuntimeThreadSuspended(threadId2, new EventInfo(3, pid, threadId1));
            mainThread.Execute(4, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Equal(2, shadowCLR.Threads.Count);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId1, out var shadowThread1) && mainThread == shadowThread1);
            Assert.Equal(ShadowRuntimeState.Suspended, shadowCLR.State);
            Assert.Equal(ShadowThreadState.Suspended, shadowCLR.Threads[threadId2].State);
            Assert.True(shadowCLR.SuspensionReason.HasValue);
            Assert.Equal(reason, shadowCLR.SuspensionReason);
            Assert.True(profilerInitializedRaised);
            Assert.True(threadCreatedRaised);
            Assert.True(runtimeSuspendStartedRaised);
            Assert.True(runtimeThreadSuspendedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_RuntimeThreadResumedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId1 = new(456);
            UIntPtr threadId2 = new(789);
            ShadowThread mainThread;
            var reason = COR_PRF_SUSPEND_REASON.GC;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var threadCreatedRaised = false;
            var runtimeSuspendStartedRaised = false;
            var runtimeThreadSuspendedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.ThreadCreated += _ => threadCreatedRaised = true;
            eventsHub.RuntimeSuspendStarted += _ => runtimeSuspendStartedRaised = true;
            eventsHub.RuntimeThreadSuspended += _ => runtimeThreadSuspendedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId1));
            scheduler.Schedule_ThreadCreated(threadId2, new EventInfo(1, pid, threadId1));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_RuntimeSuspendStarted(reason, new EventInfo(2, pid, threadId1));
            scheduler.Schedule_RuntimeThreadSuspended(threadId2, new EventInfo(3, pid, threadId1));
            scheduler.Schedule_RuntimeThreadResumed(threadId2, new EventInfo(4, pid, threadId1));
            mainThread.Execute(5, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Equal(2, shadowCLR.Threads.Count);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId1, out var shadowThread1) && mainThread == shadowThread1);
            Assert.Equal(ShadowRuntimeState.Suspended, shadowCLR.State);
            Assert.Equal(ShadowThreadState.Running, shadowCLR.Threads[threadId2].State);
            Assert.True(shadowCLR.SuspensionReason.HasValue);
            Assert.Equal(reason, shadowCLR.SuspensionReason);
            Assert.True(profilerInitializedRaised);
            Assert.True(threadCreatedRaised);
            Assert.True(runtimeSuspendStartedRaised);
            Assert.True(runtimeThreadSuspendedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_GarbageCollectionStartedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            ShadowThread mainThread;
            var reason = COR_PRF_SUSPEND_REASON.GC;
            var generations = new bool[] { true, false, false };
            var bounds = new[]
            {
                new COR_PRF_GC_GENERATION_RANGE()
                {
                    generation = COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_0,
                    rangeStart = new UIntPtr(0),
                    rangeLength = new UIntPtr(8),
                    rangeLengthReserved = new UIntPtr(16)
                }
            };
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var runtimeSuspendStartedRaised = false;
            var runtimeThreadSuspendedRaised = false;
            var garbageCollectionStartedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.RuntimeSuspendStarted += _ => runtimeSuspendStartedRaised = true;
            eventsHub.RuntimeThreadSuspended += _ => runtimeThreadSuspendedRaised = true;
            eventsHub.GarbageCollectionStarted += _ => garbageCollectionStartedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_RuntimeSuspendStarted(reason, new EventInfo(1, pid, threadId));
            scheduler.Schedule_RuntimeThreadSuspended(threadId, new EventInfo(2, pid, threadId));
            scheduler.Schedule_GarbageCollectionStarted(generations, bounds, new EventInfo(3, pid, threadId));
            mainThread.Execute(5, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Single(shadowCLR.Threads);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId, out var shadowThread1) && mainThread == shadowThread1);
            Assert.Equal(ShadowRuntimeState.Suspended, shadowCLR.State);
            Assert.True(shadowCLR.SuspensionReason.HasValue);
            Assert.Equal(reason, shadowCLR.SuspensionReason);
            Assert.True(profilerInitializedRaised);
            Assert.True(runtimeSuspendStartedRaised);
            Assert.True(runtimeThreadSuspendedRaised);
            Assert.True(garbageCollectionStartedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_GarbageCollectionFinishedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            ShadowThread mainThread;
            var reason = COR_PRF_SUSPEND_REASON.GC;
            var generations = new bool[] { true, false, false };
            var bounds = new[]
            {
                new COR_PRF_GC_GENERATION_RANGE()
                {
                    generation = COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_0,
                    rangeStart = new UIntPtr(0),
                    rangeLength = new UIntPtr(8),
                    rangeLengthReserved = new UIntPtr(16)
                }
            };
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var runtimeSuspendStartedRaised = false;
            var runtimeThreadSuspendedRaised = false;
            var garbageCollectionStartedRaised = false;
            var garbageCollectionFinishedRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.RuntimeSuspendStarted += _ => runtimeSuspendStartedRaised = true;
            eventsHub.RuntimeThreadSuspended += _ => runtimeThreadSuspendedRaised = true;
            eventsHub.GarbageCollectionStarted += _ => garbageCollectionStartedRaised = true;
            eventsHub.GarbageCollectionFinished += _ => garbageCollectionFinishedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_RuntimeSuspendStarted(reason, new EventInfo(1, pid, threadId));
            scheduler.Schedule_RuntimeThreadSuspended(threadId, new EventInfo(2, pid, threadId));
            scheduler.Schedule_GarbageCollectionStarted(generations, bounds, new EventInfo(3, pid, threadId));
            scheduler.Schedule_GarbageCollectionFinished(bounds, new EventInfo(4, pid, threadId));
            mainThread.Execute(5, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Single(shadowCLR.Threads);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId, out var shadowThread1) && mainThread == shadowThread1);
            Assert.Equal(ShadowRuntimeState.Suspended, shadowCLR.State);
            Assert.True(shadowCLR.SuspensionReason.HasValue);
            Assert.Equal(reason, shadowCLR.SuspensionReason);
            Assert.True(profilerInitializedRaised);
            Assert.True(runtimeSuspendStartedRaised);
            Assert.True(runtimeThreadSuspendedRaised);
            Assert.True(garbageCollectionStartedRaised);
            Assert.True(garbageCollectionFinishedRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_SurvivingReferencesAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            ShadowThread mainThread;
            var reason = COR_PRF_SUSPEND_REASON.GC;
            var generations = new bool[] { true, false, false };
            var bounds = new[]
            {
                new COR_PRF_GC_GENERATION_RANGE()
                {
                    generation = COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_0,
                    rangeStart = new UIntPtr(0),
                    rangeLength = new UIntPtr(16),
                    rangeLengthReserved = new UIntPtr(32)
                }
            };
            var survivingStarts = new UIntPtr[] { new UIntPtr(0) };
            var survivingLengths = new UIntPtr[] { new UIntPtr(8) };
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var runtimeSuspendStartedRaised = false;
            var runtimeThreadSuspendedRaised = false;
            var garbageCollectionStartedRaised = false;
            var survivingReferencesRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.RuntimeSuspendStarted += _ => runtimeSuspendStartedRaised = true;
            eventsHub.RuntimeThreadSuspended += _ => runtimeThreadSuspendedRaised = true;
            eventsHub.GarbageCollectionStarted += _ => garbageCollectionStartedRaised = true;
            eventsHub.SurvivingReferences += _ => survivingReferencesRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_RuntimeSuspendStarted(reason, new EventInfo(1, pid, threadId));
            scheduler.Schedule_RuntimeThreadSuspended(threadId, new EventInfo(2, pid, threadId));
            scheduler.Schedule_GarbageCollectionStarted(generations, bounds, new EventInfo(3, pid, threadId));
            scheduler.Schedule_SurvivingReferences(survivingStarts, survivingLengths, new EventInfo(4, pid, threadId));
            mainThread.Execute(5, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Single(shadowCLR.Threads);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId, out var shadowThread1) && mainThread == shadowThread1);
            Assert.Equal(ShadowRuntimeState.Suspended, shadowCLR.State);
            Assert.True(shadowCLR.SuspensionReason.HasValue);
            Assert.Equal(reason, shadowCLR.SuspensionReason);
            Assert.True(profilerInitializedRaised);
            Assert.True(runtimeSuspendStartedRaised);
            Assert.True(runtimeThreadSuspendedRaised);
            Assert.True(garbageCollectionStartedRaised);
            Assert.True(survivingReferencesRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_MovedReferencesAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            ShadowThread mainThread;
            var reason = COR_PRF_SUSPEND_REASON.GC;
            var generations = new bool[] { true, false, false };
            var bounds = new[]
            {
                new COR_PRF_GC_GENERATION_RANGE()
                {
                    generation = COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_0,
                    rangeStart = new UIntPtr(0),
                    rangeLength = new UIntPtr(16),
                    rangeLengthReserved = new UIntPtr(32)
                }
            };
            var oldStarts = new UIntPtr[] { new UIntPtr(8) };
            var newStarts = new UIntPtr[] { new UIntPtr(16) };
            var lengths = new UIntPtr[] { new UIntPtr(8) };
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var executionFinished = new TaskCompletionSource();

            // Act
            var profilerInitializedRaised = false;
            var runtimeSuspendStartedRaised = false;
            var runtimeThreadSuspendedRaised = false;
            var garbageCollectionStartedRaised = false;
            var movedReferencesRaised = false;
            eventsHub.ProfilerInitialized += _ => profilerInitializedRaised = true;
            eventsHub.RuntimeSuspendStarted += _ => runtimeSuspendStartedRaised = true;
            eventsHub.RuntimeThreadSuspended += _ => runtimeThreadSuspendedRaised = true;
            eventsHub.GarbageCollectionStarted += _ => garbageCollectionStartedRaised = true;
            eventsHub.MovedReferences += _ => movedReferencesRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_RuntimeSuspendStarted(reason, new EventInfo(1, pid, threadId));
            scheduler.Schedule_RuntimeThreadSuspended(threadId, new EventInfo(2, pid, threadId));
            scheduler.Schedule_GarbageCollectionStarted(generations, bounds, new EventInfo(3, pid, threadId));
            scheduler.Schedule_MovedReferences(oldStarts, newStarts, lengths, new EventInfo(4, pid, threadId));
            mainThread.Execute(5, JobFlags.Concurrent, new Task(() => executionFinished.SetResult()));
            await executionFinished.Task;

            // Assert
            Assert.Single(shadowCLR.Threads);
            Assert.True(shadowCLR.Threads.TryGetValue(threadId, out var shadowThread1) && mainThread == shadowThread1);
            Assert.Equal(ShadowRuntimeState.Suspended, shadowCLR.State);
            Assert.True(shadowCLR.SuspensionReason.HasValue);
            Assert.Equal(reason, shadowCLR.SuspensionReason);
            Assert.True(profilerInitializedRaised);
            Assert.True(runtimeSuspendStartedRaised);
            Assert.True(runtimeThreadSuspendedRaised);
            Assert.True(garbageCollectionStartedRaised);
            Assert.True(movedReferencesRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_WatchdogStarvedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(321);
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var crashed = false;
            using var signaller = new ManualResetEvent(false);
            scheduler.ProcessCrashed += () =>
            {
                crashed = true;
                signaller.Set();
            };

            // Act
            var heartBeatRaised = false;
            eventsHub.Heartbeat += _ => heartBeatRaised = true;
            await Task.WhenAny(Task.Delay(SchedulerBase.MaximumDelayBetweenHeartbeats * 2 + TimeSpan.FromMilliseconds(500)), Task.Run(() => signaller.WaitOne()));

            // Assert
            Assert.True(crashed);
            Assert.False(heartBeatRaised);
        }

        [Fact]
        public async Task Scheduller_ProfilingEvent_WatchdogFedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(321);
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, methodDataRegistry, metadataContext, new UtcDateTimeProvider());
            var crashed = false;
            scheduler.ProcessCrashed += () => crashed = true;

            // Act
            var heartBeatRaisesCount = 0;
            eventsHub.Heartbeat += _ => heartBeatRaisesCount++;
            var delayTask = Task.Delay(TimeSpan.FromSeconds(1));
            for (var i = 1; i < SchedulerBase.MaximumDelayBetweenHeartbeats.Seconds; i++)
            {
                delayTask = delayTask.ContinueWith(_ =>
                    // Send heartbeat
                    scheduler.Schedule_Heartbeat(new((ulong)i, pid, threadId))).ContinueWith(_ =>
                    // Wait one more second
                    Task.Delay(TimeSpan.FromSeconds(1)));
            }
            await delayTask;

            // Assert
            Assert.False(crashed);
            Assert.Equal(SchedulerBase.MaximumDelayBetweenHeartbeats.TotalSeconds - 1, heartBeatRaisesCount);
        }
    }
}
