using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Plugins;
using SharpDetect.Core;
using SharpDetect.IntegrationTests.Mocks;
using System.Collections.Concurrent;
using Xunit;

namespace SharpDetect.IntegrationTests
{
    public class BasicEventTests : IClassFixture<EnvironmentFixture>
    {
        private readonly EnvironmentFixture environment;

        public BasicEventTests(EnvironmentFixture fixture)
        {
            environment = fixture;
        }

        [Fact]
        public async Task BasicEventTests_AnalysisBeginsAndTerminates()
        {
            // Prepare
            using var scope = environment.ServiceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            var configuration = provider.GetRequiredService<IConfiguration>();
            var analysis = provider.GetRequiredService<IAnalysis>();
            var sink = provider.GetRequiredService<BlockingCollection<string>>();
            using var profiler = new MockProfiler(configuration);

            // Act
            profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 2,
                ProcessId = 123,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysis.ExecuteAsync(CancellationToken.None));
            Assert.Equal(2, sink.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), sink.First());
            Assert.Equal(nameof(IPlugin.AnalysisEnded), sink.Skip(1).First());
        }

        [Fact]
        public async Task BasicEventTests_ThreadCreatedAndDestroyed()
        {
            // Prepare
            using var scope = environment.ServiceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            var configuration = provider.GetRequiredService<IConfiguration>();
            var analysis = provider.GetRequiredService<IAnalysis>();
            var sink = provider.GetRequiredService<BlockingCollection<string>>();
            using var profiler = new MockProfiler(configuration);

            // Act
            profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                ThreadCreated = new Notify_ThreadCreated()
                {
                    ThreadId = 456
                },
                NotificationId = 2,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                ThreadDestroyed = new Notify_ThreadDestroyed()
                {
                    ThreadId = 456
                },
                NotificationId = 3,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 4,
                ProcessId = 123,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysis.ExecuteAsync(CancellationToken.None));
            Assert.Equal(4, sink.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), sink.First());
            Assert.Equal(nameof(IPlugin.ThreadCreated), sink.Skip(1).First());
            Assert.Equal(nameof(IPlugin.ThreadDestroyed), sink.Skip(2).First());
            Assert.Equal(nameof(IPlugin.AnalysisEnded), sink.Skip(3).First());
        }

        [Fact]
        public async Task BasicEventTests_ModuleLoadedAndTypeLoaded()
        {
            // Prepare
            using var scope = environment.ServiceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            var configuration = provider.GetRequiredService<IConfiguration>();

            using var profiler = new MockProfiler(configuration);
            var analysis = provider.GetRequiredService<IAnalysis>();
            var sink = provider.GetRequiredService<BlockingCollection<string>>();

            // Act
            profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                ModuleLoaded = new Notify_ModuleLoaded()
                {
                    ModuleId = 456,
                    ModulePath = typeof(BasicEventTests).Assembly.Location
                },
                NotificationId = 2,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                TypeLoaded = new Notify_TypeLoaded()
                {
                    ModuleId = 456,
                    TypeToken = (uint)typeof(BasicEventTests).MetadataToken
                },
                NotificationId = 3,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 4,
                ProcessId = 123,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysis.ExecuteAsync(CancellationToken.None));
            Assert.Equal(4, sink.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), sink.First());
            Assert.Equal(nameof(IPlugin.ModuleLoaded), sink.Skip(1).First());
            Assert.Equal(nameof(IPlugin.TypeLoaded), sink.Skip(2).First());
            Assert.Equal(nameof(IPlugin.AnalysisEnded), sink.Skip(3).First());
        }

        [Fact]
        public async Task BasicEventTests_JITCompilationStarted()
        {
            // Prepar
            using var scope = environment.ServiceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            var configuration = provider.GetRequiredService<IConfiguration>();

            using var profiler = new MockProfiler(configuration);
            var analysis = provider.GetRequiredService<IAnalysis>();
            var sink = provider.GetRequiredService<BlockingCollection<string>>();

            // Act
            profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                ModuleLoaded = new Notify_ModuleLoaded()
                {
                    ModuleId = 456,
                    ModulePath = typeof(BasicEventTests).Assembly.Location
                },
                NotificationId = 2,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                TypeLoaded = new Notify_TypeLoaded()
                {
                    ModuleId = 456,
                    TypeToken = (uint)typeof(BasicEventTests).MetadataToken
                },
                NotificationId = 3,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                JITCompilationStarted = new Notify_JITCompilationStarted()
                {
                    ModuleId = 456,
                    TypeToken = (uint)typeof(BasicEventTests).MetadataToken,
                    FunctionToken = (uint)typeof(BasicEventTests)
                        .GetMethod(nameof(BasicEventTests_JITCompilationStarted))!.MetadataToken
                },
                NotificationId = 4,
                ProcessId = 123,
                ThreadId = 0,
            });
            profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 5,
                ProcessId = 123,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysis.ExecuteAsync(CancellationToken.None));
            Assert.Equal(5, sink.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), sink.First());
            Assert.Equal(nameof(IPlugin.ModuleLoaded), sink.Skip(1).First());
            Assert.Equal(nameof(IPlugin.TypeLoaded), sink.Skip(2).First());
            Assert.Equal(nameof(IPlugin.JITCompilationStarted), sink.Skip(3).First());
            Assert.Equal(nameof(IPlugin.AnalysisEnded), sink.Skip(4).First());
        }
    }
}