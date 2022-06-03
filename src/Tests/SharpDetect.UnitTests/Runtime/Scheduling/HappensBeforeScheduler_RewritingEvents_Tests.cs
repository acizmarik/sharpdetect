﻿using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Services;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Runtime.Scheduling;
using Xunit;

namespace SharpDetect.UnitTests.Runtime.Scheduling
{
    public class HappensBeforeScheduler_RewritingEvents_Tests : TestsBase
    {
        [Fact]
        public async Task Scheduller_RewritingEvent_TypeInjected()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var typeMDToken = new MDToken(987);
            var moduleBindContext = ModuleBindContext;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var typeInjectedRaised = false;
            eventsHub.TypeInjected += _ => typeInjectedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            var mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new EventInfo(1, pid, threadId));
            scheduler.Schedule_TypeInjected(new(moduleId, typeMDToken), new EventInfo(2, pid, threadId));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(typeInjectedRaised);
        }

        [Fact]
        public async Task Scheduller_RewritingEvent_MethodInjected()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var typeMDToken = new MDToken(987);
            var functionMDToken = new MDToken(654);
            var moduleBindContext = ModuleBindContext;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var methodInjectedRaised = false;
            eventsHub.MethodInjected += _ => methodInjectedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            var mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new EventInfo(1, pid, threadId));
            scheduler.Schedule_MethodInjected(new(moduleId, typeMDToken, functionMDToken), Common.Messages.MethodType.FieldAccess, new EventInfo(2, pid, threadId));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodInjectedRaised);
        }

        [Fact]
        public async Task Scheduller_RewritingEvent_MethodWrapperInjected()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = typeof(Monitor).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var externFunctionMDToken = new MDToken(typeof(Monitor).GetMethod(nameof(Monitor.Exit))!.MetadataToken);
            var wrapperFunctionMDToken = new MDToken(321);
            var moduleBindContext = ModuleBindContext;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var methodWrapperInjectedRaised = false;
            eventsHub.MethodWrapperInjected += _ => methodWrapperInjectedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            var mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new EventInfo(1, pid, threadId));
            scheduler.Schedule_WrapperInjected(new(moduleId, typeMDToken, externFunctionMDToken), wrapperFunctionMDToken, new EventInfo(2, pid, threadId));
            mainThread.Execute(3, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodWrapperInjectedRaised);
        }

        [Fact(Skip = "NotYetImplemented")]
        public Task Scheduller_RewritingEvent_TypeReferenced()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Scheduller_RewritingEvent_HelperMethodReferenced()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr moduleId = new(789);
            var modulePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var typeMDToken = new MDToken(987);
            var functionMDToken = new MDToken(654);
            var referenceFunctionMDToken = new MDToken(321);
            var moduleBindContext = ModuleBindContext;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var methodInjectedRaised = false;
            var helperMethodReferencedRaised = false;
            eventsHub.MethodInjected += _ => methodInjectedRaised = true;
            eventsHub.HelperMethodReferenced += _ => helperMethodReferencedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            var mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(moduleId, modulePath, new EventInfo(1, pid, threadId));
            scheduler.Schedule_MethodInjected(new(moduleId, typeMDToken, functionMDToken), Common.Messages.MethodType.FieldAccess, new EventInfo(2, pid, threadId));
            scheduler.Schedule_HelperReferenced(new(moduleId, typeMDToken, referenceFunctionMDToken), Common.Messages.MethodType.FieldAccess, new EventInfo(3, pid, threadId));
            mainThread.Execute(4, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodInjectedRaised);
            Assert.True(helperMethodReferencedRaised);
        }

        [Fact]
        public async Task Scheduller_RewritingEvent_WrapperMethodReferenced()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            UIntPtr definitionModuleId = new(789);
            UIntPtr referenceModuleId = new(978);
            var definitionModulePath = typeof(Monitor).Assembly.Location;
            var referenceModulePath = typeof(HappensBeforeScheduler_RewritingEvents_Tests).Assembly.Location;
            var typeMDToken = new MDToken(typeof(Monitor).MetadataToken);
            var externFunctionMDToken = new MDToken(typeof(Monitor).GetMethod(nameof(Monitor.Exit))!.MetadataToken);
            var wrapperFunctionMDToken = new MDToken(321);
            var wrapperReferenceFunctionMDToken = new MDToken(654);
            var moduleBindContext = ModuleBindContext;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var shadowCLR = InitiateDotnetProcessProfiling(pid, profilingMessageHub, moduleBindContext, metadataContext);
            var eventsHub = new RuntimeEventsHub();
            using var scheduler = new HappensBeforeScheduler(pid, shadowCLR, eventsHub, new UtcDateTimeProvider());
            var executionCompletion = new TaskCompletionSource();

            // Act
            var methodWrapperInjectedRaised = false;
            var methodWrapperReferencedRaised = false;
            eventsHub.MethodWrapperInjected += _ => methodWrapperInjectedRaised = true;
            eventsHub.WrapperMethodReferenced += _ => methodWrapperReferencedRaised = true;
            scheduler.Schedule_ProfilerInitialized(new EventInfo(0, pid, threadId));
            var mainThread = scheduler.ShadowThreads.First();
            scheduler.Schedule_ModuleLoaded(definitionModuleId, definitionModulePath, new EventInfo(1, pid, threadId));
            scheduler.Schedule_ModuleLoaded(referenceModuleId, referenceModulePath, new EventInfo(2, pid, threadId));
            scheduler.Schedule_WrapperInjected(new(definitionModuleId, typeMDToken, externFunctionMDToken), wrapperFunctionMDToken, new EventInfo(3, pid, threadId));
            scheduler.Schedule_WrapperReferenced(new(definitionModuleId, typeMDToken, externFunctionMDToken), new(definitionModuleId, typeMDToken, wrapperReferenceFunctionMDToken), new EventInfo(4, pid, threadId));
            mainThread.Execute(5, JobFlags.Concurrent, new Task(() => executionCompletion.SetResult()));
            await executionCompletion.Task;

            // Assert
            Assert.True(methodWrapperInjectedRaised);
            Assert.True(methodWrapperReferencedRaised);
        }
    }
}