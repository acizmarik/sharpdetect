using SharpDetect.Common.Messages;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Models;
using SharpDetect.Core.Runtime;
using Xunit;

namespace SharpDetect.UnitTests.Runtime
{
    public class ShadowExecutionTests : TestsBase
    {
        [Fact]
        public void ShadowExecution_SingleProcess_Created()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            var profilingHub = new ProfilingMessageHub(LoggerFactory);
            var rewritingHub = new RewritingMessageHub(LoggerFactory);
            var moduleContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(ModuleBindContext, profilingHub);
            var methodRegistry = new MethodDescriptorRegistry();
            var executingHub = new ExecutingMessageHub(metadataContext, methodRegistry, LoggerFactory);
            var provider = BuildServiceProvider((typeof(IModuleBindContext), moduleContext));
            using var execution = new ShadowExecution(new RuntimeEventsHub(), profilingHub, rewritingHub, executingHub, metadataContext, LoggerFactory, provider);

            // Act
            profilingHub.Process(new NotifyMessage()
            {
                NotificationId = 0,
                ProcessId = pid,
                ThreadId = threadId.ToUInt64(),
                ProfilerInitialized = new Notify_ProfilerInitialized()
            });

            // Assert
            Assert.Single(execution.Schedulers);
        }

        [Fact]
        public async Task ShadowExecution_SingleProcess_TerminatedAsync()
        {
            // Prepare
            const int pid = 123;
            UIntPtr threadId = new(456);
            var profilingHub = new ProfilingMessageHub(LoggerFactory);
            var rewritingHub = new RewritingMessageHub(LoggerFactory);
            var moduleContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(ModuleBindContext, profilingHub);
            var methodRegistry = new MethodDescriptorRegistry();
            var executingHub = new ExecutingMessageHub(metadataContext, methodRegistry, LoggerFactory);
            var provider = BuildServiceProvider((typeof(IModuleBindContext), moduleContext));
            using var execution = new ShadowExecution(new RuntimeEventsHub(), profilingHub, rewritingHub, executingHub, metadataContext, LoggerFactory, provider);
            var executionFinished = execution.GetAwaitableTaskAsync();

            // Act
            profilingHub.Process(new NotifyMessage()
            {
                NotificationId = 0,
                ProcessId = pid,
                ThreadId = threadId.ToUInt64(),
                ProfilerInitialized = new Notify_ProfilerInitialized()
            });
            profilingHub.Process(new NotifyMessage()
            {
                NotificationId = 0,
                ProcessId = pid,
                ThreadId = threadId.ToUInt64(),
                ProfilerDestroyed = new Notify_ProfilerDestroyed()
            });
            await executionFinished;

            // Assert
            Assert.Empty(execution.Schedulers);
        }

        [Fact]
        public void ShadowExecution_MultiProcess_Created()
        {
            // Prepare
            const int pid1 = 123;
            const int pid2 = 456;
            UIntPtr threadId = new(789);
            var profilingHub = new ProfilingMessageHub(LoggerFactory);
            var rewritingHub = new RewritingMessageHub(LoggerFactory);
            var moduleContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(ModuleBindContext, profilingHub);
            var methodRegistry = new MethodDescriptorRegistry();
            var executingHub = new ExecutingMessageHub(metadataContext, methodRegistry, LoggerFactory);
            var provider = BuildServiceProvider((typeof(IModuleBindContext), moduleContext));
            using var execution = new ShadowExecution(new RuntimeEventsHub(), profilingHub, rewritingHub, executingHub, metadataContext, LoggerFactory, provider);

            // Act
            profilingHub.Process(new NotifyMessage()
            {
                NotificationId = 0,
                ProcessId = pid1,
                ThreadId = threadId.ToUInt64(),
                ProfilerInitialized = new Notify_ProfilerInitialized()
            });
            profilingHub.Process(new NotifyMessage()
            {
                NotificationId = 0,
                ProcessId = pid2,
                ThreadId = threadId.ToUInt64(),
                ProfilerInitialized = new Notify_ProfilerInitialized()
            });

            // Assert
            Assert.Equal(2, execution.Schedulers.Count());
        }

        [Fact]
        public async Task ShadowExecution_MultiProcess_TerminatedAsync()
        {
            // Prepare
            const int pid1 = 123;
            const int pid2 = 456;
            UIntPtr threadId = new(789);
            var profilingHub = new ProfilingMessageHub(LoggerFactory);
            var rewritingHub = new RewritingMessageHub(LoggerFactory);
            var moduleContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(ModuleBindContext, profilingHub);
            var methodRegistry = new MethodDescriptorRegistry();
            var executingHub = new ExecutingMessageHub(metadataContext, methodRegistry, LoggerFactory);
            var provider = BuildServiceProvider((typeof(IModuleBindContext), moduleContext));
            using var execution = new ShadowExecution(new RuntimeEventsHub(), profilingHub, rewritingHub, executingHub, metadataContext, LoggerFactory, provider);
            var executionFinished = execution.GetAwaitableTaskAsync();

            // Act
            profilingHub.Process(new NotifyMessage()
            {
                NotificationId = 0,
                ProcessId = pid1,
                ThreadId = threadId.ToUInt64(),
                ProfilerInitialized = new Notify_ProfilerInitialized()
            });
            profilingHub.Process(new NotifyMessage()
            {
                NotificationId = 0,
                ProcessId = pid2,
                ThreadId = threadId.ToUInt64(),
                ProfilerInitialized = new Notify_ProfilerInitialized()
            });
            profilingHub.Process(new NotifyMessage()
            {
                NotificationId = 1,
                ProcessId = pid1,
                ThreadId = threadId.ToUInt64(),
                ProfilerDestroyed = new Notify_ProfilerDestroyed()
            });
            profilingHub.Process(new NotifyMessage()
            {
                NotificationId = 1,
                ProcessId = pid2,
                ThreadId = threadId.ToUInt64(),
                ProfilerDestroyed = new Notify_ProfilerDestroyed()
            });
            await executionFinished;

            // Assert
            Assert.Empty(execution.Schedulers);
        }
    }
}
