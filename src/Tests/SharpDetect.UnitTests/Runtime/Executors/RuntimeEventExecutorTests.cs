// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Runtime.Executors;
using SharpDetect.Core.Runtime.Threads;
using Xunit;

namespace SharpDetect.UnitTests.Runtime.Executors
{
    public class RuntimeEventExecutorTests : TestsBase
    {
        [Fact]
        public async Task RuntimeEventExecutor_Setup()
        {
            // Prepare
            const int pid = 123;
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var _ = new RuntimeEventExecutor(pid, shadowCLR, new(), metadataContext, profilingClient, methodRegistry);

            // Assert
            Assert.Equal(ShadowRuntimeState.Initiated, shadowCLR.State);
        }

        [Fact]
        public async Task RuntimeEventExecutor_ProfilerInitialized()
        {
            // Prepare
            const int pid = 123;
            const nuint tid = 456;
            var eventHub = new RuntimeEventsHub();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var thread = new ShadowThread(pid, tid, 1, new NullLoggerFactory(), new EpochSource());
            var evtRaised = new TaskCompletionSource();
            eventHub.ProfilerInitialized += (_) => evtRaised.SetResult();

            // Act
            executor.ExecuteProfilerInitialized(thread, new(1, pid, tid));
            await evtRaised.Task;

            // Assert
            Assert.True(evtRaised.Task.IsCompletedSuccessfully);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
        }

        [Fact]
        public async Task RuntimeEventExecutor_ProfilerDestroyed()
        {
            // Prepare
            const int pid = 123;
            const nuint tid = 456;
            var eventHub = new RuntimeEventsHub();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var thread = new ShadowThread(pid, tid, 1, new NullLoggerFactory(), new EpochSource());
            var evtRaised = new TaskCompletionSource();
            eventHub.ProfilerDestroyed += (_) => evtRaised.SetResult();

            // Act
            executor.ExecuteProfilerInitialized(thread, new(1, pid, tid));
            executor.ExecuteProfilerDestroyed(new(2, pid, tid));
            await evtRaised.Task;

            // Assert
            Assert.True(evtRaised.Task.IsCompletedSuccessfully);
            Assert.Equal(ShadowRuntimeState.Terminated, shadowCLR.State);
        }

        [Fact]
        public async Task RuntimeEventExecutor_ModuleLoaded()
        {
            // Prepare
            const int pid = 123;
            const nuint tid = 456;
            const nuint mid = 789;
            var eventHub = new RuntimeEventsHub();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var thread = new ShadowThread(pid, tid, 1, new NullLoggerFactory(), new EpochSource());
            var evtRaised = new TaskCompletionSource();
            eventHub.ModuleLoaded += (_) => evtRaised.SetResult();

            // Act
            executor.ExecuteProfilerInitialized(thread, new(1, pid, tid));
            executor.ExecuteModuleLoaded(mid, typeof(RuntimeEventExecutor).Assembly.Location, new(2, pid, tid));
            await evtRaised.Task;

            // Assert
            Assert.True(evtRaised.Task.IsCompletedSuccessfully);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(moduleBindContext.TryGetModule(pid, new(mid), out var module));
            Assert.Equal(typeof(RuntimeEventExecutor).Assembly.ManifestModule.Name, module.FullName);
        }

        [Fact]
        public async Task RuntimeEventExecutor_TypeLoaded()
        {
            // Prepare
            const int pid = 123;
            const nuint tid = 456;
            const nuint mid = 789;
            var eventHub = new RuntimeEventsHub();
            MDToken typeToken = new(typeof(RuntimeEventExecutor).MetadataToken);
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var thread = new ShadowThread(pid, tid, 1, new NullLoggerFactory(), new EpochSource());
            var evtRaised = new TaskCompletionSource();
            eventHub.TypeLoaded += (_) => evtRaised.SetResult();

            // Act
            executor.ExecuteProfilerInitialized(thread, new(1, pid, tid));
            executor.ExecuteModuleLoaded(mid, typeof(RuntimeEventExecutor).Assembly.Location, new(2, pid, tid));
            executor.ExecuteTypeLoaded(new(mid, typeToken), new(3, pid, tid));
            await evtRaised.Task;

            // Assert
            Assert.True(evtRaised.Task.IsCompletedSuccessfully);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(moduleBindContext.TryGetModule(pid, new(mid), out var module));
            Assert.Equal(typeof(RuntimeEventExecutor).Assembly.ManifestModule.Name, module.FullName);
        }

        [Fact]
        public async Task RuntimeEventExecutor_JITCompilationStarted_WithTypeLoaded()
        {
            // Prepare
            const int pid = 123;
            const nuint tid = 456;
            const nuint mid = 789;
            var eventHub = new RuntimeEventsHub();
            MDToken typeToken = new(typeof(RuntimeEventExecutor).MetadataToken);
            MDToken functionToken = new(typeof(RuntimeEventExecutor).GetMethod(nameof(RuntimeEventExecutor.ExecuteJITCompilationStarted))!.MetadataToken);
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var thread = new ShadowThread(pid, tid, 1, new NullLoggerFactory(), new EpochSource());
            var evtRaised = new TaskCompletionSource();
            eventHub.JITCompilationStarted += (_) => evtRaised.SetResult();

            // Act
            executor.ExecuteProfilerInitialized(thread, new(1, pid, tid));
            executor.ExecuteModuleLoaded(mid, typeof(RuntimeEventExecutor).Assembly.Location, new(2, pid, tid));
            executor.ExecuteTypeLoaded(new(mid, typeToken), new(3, pid, tid));
            executor.ExecuteJITCompilationStarted(new(mid, typeToken, functionToken), new(4, pid, tid));
            await evtRaised.Task;

            // Assert
            Assert.True(evtRaised.Task.IsCompletedSuccessfully);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(moduleBindContext.TryGetModule(pid, new(mid), out var module));
            Assert.Equal(typeof(RuntimeEventExecutor).Assembly.ManifestModule.Name, module.FullName);
        }

        [Fact]
        public async Task RuntimeEventExecutor_JITCompilationStarted_WithoutTypeLoaded()
        {
            // Prepare
            const int pid = 123;
            const nuint tid = 456;
            const nuint mid = 789;
            var eventHub = new RuntimeEventsHub();
            MDToken typeToken = new(typeof(RuntimeEventExecutor).MetadataToken);
            MDToken functionToken = new(typeof(RuntimeEventExecutor).GetMethod(nameof(RuntimeEventExecutor.ExecuteJITCompilationStarted))!.MetadataToken);
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var thread = new ShadowThread(pid, tid, 1, new NullLoggerFactory(), new EpochSource());
            var evtRaised = new TaskCompletionSource();
            eventHub.JITCompilationStarted += (_) => evtRaised.SetResult();

            // Act
            executor.ExecuteProfilerInitialized(thread, new(1, pid, tid));
            executor.ExecuteModuleLoaded(mid, typeof(RuntimeEventExecutor).Assembly.Location, new(2, pid, tid));
            executor.ExecuteJITCompilationStarted(new(mid, typeToken, functionToken), new(3, pid, tid));
            await evtRaised.Task;

            // Assert
            Assert.True(evtRaised.Task.IsCompletedSuccessfully);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
            Assert.True(moduleBindContext.TryGetModule(pid, new(mid), out var module));
            Assert.Equal(typeof(RuntimeEventExecutor).Assembly.ManifestModule.Name, module.FullName);
        }

        [Fact]
        public async Task RuntimeEventExecutor_ThreadCreated()
        {
            // Prepare
            const int pid = 123;
            const nuint tid1 = 456;
            const nuint tid2 = 789;
            var eventHub = new RuntimeEventsHub();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var thread1 = new ShadowThread(pid, tid1, 1, new NullLoggerFactory(), new EpochSource());
            var thread2 = new ShadowThread(pid, tid2, 2, new NullLoggerFactory(), new EpochSource());
            var evtRaised = new TaskCompletionSource();
            eventHub.ThreadCreated += (_) => evtRaised.SetResult();

            // Act
            executor.ExecuteProfilerInitialized(thread1, new(1, pid, tid1));
            executor.ExecuteThreadCreated(thread2, new(2, pid, tid2));
            await evtRaised.Task;

            // Assert
            Assert.True(evtRaised.Task.IsCompletedSuccessfully);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
        }

        [Fact]
        public async Task RuntimeEventExecutor_ThreadDestroyed()
        {
            // Prepare
            const int pid = 123;
            const nuint tid1 = 456;
            const nuint tid2 = 789;
            var eventHub = new RuntimeEventsHub();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var profilingClient = Moq.Mock.Of<IProfilingClient>();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var methodRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var executor = new RuntimeEventExecutor(pid, shadowCLR, eventHub, metadataContext, profilingClient, methodRegistry);
            var thread1 = new ShadowThread(pid, tid1, 1, new NullLoggerFactory(), new EpochSource());
            var thread2 = new ShadowThread(pid, tid2, 2, new NullLoggerFactory(), new EpochSource());
            var evtRaised = new TaskCompletionSource();
            eventHub.ThreadDestroyed += (_) => evtRaised.SetResult();

            // Act
            executor.ExecuteProfilerInitialized(thread1, new(1, pid, tid1));
            executor.ExecuteThreadCreated(thread2, new(2, pid, tid2));
            executor.ExecuteThreadDestroyed(thread2, new(3, pid, tid2));
            await evtRaised.Task;

            // Assert
            Assert.True(evtRaised.Task.IsCompletedSuccessfully);
            Assert.Equal(ShadowRuntimeState.Executing, shadowCLR.State);
        }
    }
}
