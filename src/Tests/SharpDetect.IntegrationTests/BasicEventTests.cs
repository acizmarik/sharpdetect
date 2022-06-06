using SharpDetect.Common.Messages;
using SharpDetect.Common.Plugins;
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
            using var session = environment.CreateAnalysisSession();
            var analysisTask = session.Start();

            // Act
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 2,
                ProcessId = 123,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysisTask);
            Assert.Equal(2, session.Output.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), session.Output.First());
            Assert.Equal(nameof(IPlugin.AnalysisEnded), session.Output.Skip(1).First());
        }

        [Fact]
        public async Task BasicEventTests_ThreadCreatedAndDestroyed()
        {
            // Prepare
            using var session = environment.CreateAnalysisSession();
            var analysisTask = session.Start();

            // Act
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ThreadCreated = new Notify_ThreadCreated()
                {
                    ThreadId = 456
                },
                NotificationId = 2,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ThreadDestroyed = new Notify_ThreadDestroyed()
                {
                    ThreadId = 456
                },
                NotificationId = 3,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 4,
                ProcessId = 123,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysisTask);
            Assert.Equal(4, session.Output.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), session.Output.First());
            Assert.Equal(nameof(IPlugin.ThreadCreated), session.Output.Skip(1).First());
            Assert.Equal(nameof(IPlugin.ThreadDestroyed), session.Output.Skip(2).First());
            Assert.Equal(nameof(IPlugin.AnalysisEnded), session.Output.Skip(3).First());
        }

        [Fact]
        public async Task BasicEventTests_JITCompilationStarted()
        {
            // Prepar
            using var session = environment.CreateAnalysisSession();
            var analysisTask = session.Start();

            // Act
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
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
            session.Profiler.Send(new NotifyMessage()
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
            session.Profiler.Send(new NotifyMessage()
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
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 5,
                ProcessId = 123,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysisTask);
            Assert.Equal(5, session.Output.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), session.Output.First());
            Assert.Equal(nameof(IPlugin.ModuleLoaded), session.Output.Skip(1).First());
            Assert.Equal(nameof(IPlugin.TypeLoaded), session.Output.Skip(2).First());
            Assert.Equal(nameof(IPlugin.JITCompilationStarted), session.Output.Skip(3).First());
            Assert.Equal(nameof(IPlugin.AnalysisEnded), session.Output.Skip(4).First());
        }
    }
}