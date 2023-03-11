// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Runtime.Executors;
using SharpDetect.Core.Runtime.Scheduling;
using Xunit;

namespace SharpDetect.UnitTests.Runtime.Schedulers
{
    public class HappensBeforeSchedulerTests : TestsBase
    {
        [Fact]
        public async Task RuntimeEventExecutor_BringAllThreadsToSuspension_EveryThreadNotified()
        {
            // Prepare
            const int pid = 123;
            var eventHub = new RuntimeEventsHub();
            var dateTimeProvider = new UtcDateTimeProvider();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var scheduler = new HappensBeforeScheduler(pid, executor, dateTimeProvider, new NullLoggerFactory());
            var evtRaised = new TaskCompletionSource();
            eventHub.RuntimeSuspendFinished += (_) => evtRaised.SetResult();

            // Act
            scheduler.Schedule_ProfilerInitialized(default, new(1, pid, 1));
            scheduler.Schedule_ThreadCreated(2, new(2, pid, 2));
            scheduler.Schedule_ThreadCreated(3, new(3, pid, 3));
            scheduler.Schedule_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON.GC, new(4, pid, 1));
            scheduler.Schedule_RuntimeThreadSuspended(1, new(5, pid, 1));
            scheduler.Schedule_RuntimeThreadSuspended(2, new(6, pid, 2));
            scheduler.Schedule_RuntimeThreadSuspended(3, new(7, pid, 3));
            scheduler.Schedule_RuntimeSuspendFinished(new(8, pid, 1));
            await evtRaised.Task;

            // Assert
            Assert.True(shadowCLR.Threads.All(t => t.Value.State == ShadowThreadState.Suspended));
        }

        [Fact]
        public async Task RuntimeEventExecutor_BringAllThreadsToSuspension_SomeThreadsNotified()
        {
            // Prepare
            const int pid = 123;
            var eventHub = new RuntimeEventsHub();
            var dateTimeProvider = new UtcDateTimeProvider();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var scheduler = new HappensBeforeScheduler(pid, executor, dateTimeProvider, new NullLoggerFactory());
            var evtRaised = new TaskCompletionSource();
            eventHub.RuntimeSuspendFinished += (_) => evtRaised.SetResult();

            // Act
            scheduler.Schedule_ProfilerInitialized(default, new(1, pid, 1));
            scheduler.Schedule_ThreadCreated(2, new(2, pid, 2));
            scheduler.Schedule_ThreadCreated(3, new(3, pid, 3));
            scheduler.Schedule_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON.GC, new(4, pid, 1));
            scheduler.Schedule_RuntimeThreadSuspended(1, new(5, pid, 1));
            scheduler.Schedule_RuntimeSuspendFinished(new(8, pid, 1));
            await evtRaised.Task;

            // Assert
            Assert.True(shadowCLR.Threads.All(t => t.Value.State == ShadowThreadState.Suspended));
        }

        [Fact]
        public async Task RuntimeEventExecutor_ResumeAllThreadsFromSuspension_AllThreadsNotified()
        {
            // Prepare
            const int pid = 123;
            var eventHub = new RuntimeEventsHub();
            var dateTimeProvider = new UtcDateTimeProvider();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var scheduler = new HappensBeforeScheduler(pid, executor, dateTimeProvider, new NullLoggerFactory());
            var evtRaised = new TaskCompletionSource();
            eventHub.RuntimeResumeFinished += (_) => evtRaised.SetResult();

            // Act
            scheduler.Schedule_ProfilerInitialized(default, new(1, pid, 1));
            scheduler.Schedule_ThreadCreated(2, new(2, pid, 2));
            scheduler.Schedule_ThreadCreated(3, new(3, pid, 3));
            scheduler.Schedule_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON.GC, new(4, pid, 1));
            scheduler.Schedule_RuntimeThreadSuspended(1, new(5, pid, 1));
            scheduler.Schedule_RuntimeSuspendFinished(new(8, pid, 1));
            scheduler.Schedule_RuntimeResumeStarted(new(9, pid, 1));
            scheduler.Schedule_RuntimeThreadResumed(1, new(10, pid, 1));
            scheduler.Schedule_RuntimeThreadResumed(2, new(11, pid, 2));
            scheduler.Schedule_RuntimeThreadResumed(3, new(12, pid, 3));
            scheduler.Schedule_RuntimeResumeFinished(new(13, pid, 1));
            await evtRaised.Task;

            // Assert
            Assert.True(shadowCLR.Threads.All(t => t.Value.State == ShadowThreadState.Running));
        }

        [Fact]
        public async Task RuntimeEventExecutor_ResumeAllThreadsFromSuspension_SomeThreadsNotified()
        {
            // Prepare
            const int pid = 123;
            var eventHub = new RuntimeEventsHub();
            var dateTimeProvider = new UtcDateTimeProvider();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var scheduler = new HappensBeforeScheduler(pid, executor, dateTimeProvider, new NullLoggerFactory());
            var evtRaised = new TaskCompletionSource();
            eventHub.RuntimeResumeFinished += (_) => evtRaised.SetResult();

            // Act
            scheduler.Schedule_ProfilerInitialized(default, new(1, pid, 1));
            scheduler.Schedule_ThreadCreated(2, new(2, pid, 2));
            scheduler.Schedule_ThreadCreated(3, new(3, pid, 3));
            scheduler.Schedule_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON.GC, new(4, pid, 1));
            scheduler.Schedule_RuntimeThreadSuspended(1, new(5, pid, 1));
            scheduler.Schedule_RuntimeSuspendFinished(new(8, pid, 1));
            scheduler.Schedule_RuntimeResumeStarted(new(9, pid, 1));
            scheduler.Schedule_RuntimeThreadResumed(1, new(10, pid, 1));
            scheduler.Schedule_RuntimeResumeFinished(new(13, pid, 1));
            await evtRaised.Task;

            // Assert
            Assert.True(shadowCLR.Threads.All(t => t.Value.State == ShadowThreadState.Running));
        }

        [Fact]
        public async Task RuntimeEventExecutor_BringAllThreadsToGarbageCollection()
        {
            // Prepare
            const int pid = 123;
            var eventHub = new RuntimeEventsHub();
            var dateTimeProvider = new UtcDateTimeProvider();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var scheduler = new HappensBeforeScheduler(pid, executor, dateTimeProvider, new NullLoggerFactory());
            var generationCollected = new[] { true, true, true, true, true };
            var generationBounds = Array.Empty<COR_PRF_GC_GENERATION_RANGE>();
            var evtRaised = new TaskCompletionSource();
            eventHub.GarbageCollectionStarted += (_) => evtRaised.SetResult();

            // Act
            scheduler.Schedule_ProfilerInitialized(default, new(1, pid, 1));
            scheduler.Schedule_ThreadCreated(2, new(2, pid, 2));
            scheduler.Schedule_ThreadCreated(3, new(3, pid, 3));
            scheduler.Schedule_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON.GC, new(4, pid, 1));
            scheduler.Schedule_RuntimeThreadSuspended(1, new(5, pid, 1));
            scheduler.Schedule_RuntimeThreadSuspended(2, new(6, pid, 2));
            scheduler.Schedule_RuntimeThreadSuspended(3, new(7, pid, 3));
            scheduler.Schedule_RuntimeSuspendFinished(new(8, pid, 1));
            scheduler.Schedule_GarbageCollectionStarted(generationCollected, generationBounds, new(9, pid, 1));
            await evtRaised.Task;

            // Assert
            Assert.True(shadowCLR.Threads.All(t => t.Value.State == ShadowThreadState.GarbageCollecting));
        }

        [Fact]
        public async Task RuntimeEventExecutor_BringAllThreadsToGarbageCollectionFinished()
        {
            // Prepare
            const int pid = 123;
            var eventHub = new RuntimeEventsHub();
            var dateTimeProvider = new UtcDateTimeProvider();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var scheduler = new HappensBeforeScheduler(pid, executor, dateTimeProvider, new NullLoggerFactory());
            var generationCollected = new[] { true, true, true, true, true };
            var generationBounds = Array.Empty<COR_PRF_GC_GENERATION_RANGE>();
            var evtRaised = new TaskCompletionSource();
            eventHub.GarbageCollectionFinished += (_) => evtRaised.SetResult();

            // Act
            scheduler.Schedule_ProfilerInitialized(default, new(1, pid, 1));
            scheduler.Schedule_ThreadCreated(2, new(2, pid, 2));
            scheduler.Schedule_ThreadCreated(3, new(3, pid, 3));
            scheduler.Schedule_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON.GC, new(4, pid, 1));
            scheduler.Schedule_RuntimeThreadSuspended(1, new(5, pid, 1));
            scheduler.Schedule_RuntimeThreadSuspended(2, new(6, pid, 2));
            scheduler.Schedule_RuntimeThreadSuspended(3, new(7, pid, 3));
            scheduler.Schedule_RuntimeSuspendFinished(new(8, pid, 1));
            scheduler.Schedule_GarbageCollectionStarted(generationCollected, generationBounds, new(9, pid, 1));
            scheduler.Schedule_GarbageCollectionFinished(generationBounds, new(10, pid, 1));
            await evtRaised.Task;

            // Assert
            Assert.True(shadowCLR.Threads.All(t => t.Value.State == ShadowThreadState.Suspended));
        }
    }
}
