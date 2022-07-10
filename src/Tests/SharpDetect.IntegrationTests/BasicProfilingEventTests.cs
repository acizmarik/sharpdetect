using SharpDetect.Common.Messages;
using SharpDetect.Common.Plugins;
using Xunit;

namespace SharpDetect.IntegrationTests
{
    [Collection("Sequential")]
    public class BasicProfilingEventTests : IClassFixture<EnvironmentFixture>
    {
        private readonly EnvironmentFixture environment;

        public BasicProfilingEventTests(EnvironmentFixture fixture)
        {
            environment = fixture;
        }

        [Fact]
        public async Task BasicProfilingEventTests_AnalysisBeginsAndTerminates()
        {
            // Prepare
            const int processId = 123;
            using var session = environment.CreateAnalysisSession(processId);
            var analysisTask = session.Start();

            // Act
            session.Profiler.Start();
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 2,
                ProcessId = processId,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysisTask);
            Assert.Equal(2, session.Output.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), session.Output.First());
            Assert.Equal(nameof(IPlugin.AnalysisEnded), session.Output.Skip(1).First());
        }

        [Fact(Skip = "CI does not like this for some reason...")]
        public async Task BasicProfilingEventTests_ThreadCreatedAndDestroyed()
        {
            // Prepare
            const int processId = 123;
            using var session = environment.CreateAnalysisSession(processId);
            var analysisTask = session.Start();

            // Act
            session.Profiler.Start();
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ThreadCreated = new Notify_ThreadCreated()
                {
                    ThreadId = 456
                },
                NotificationId = 2,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ThreadDestroyed = new Notify_ThreadDestroyed()
                {
                    ThreadId = 456
                },
                NotificationId = 3,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 4,
                ProcessId = processId,
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
        public async Task BasicProfilingEventTests_JITCompilationStarted()
        {
            // Prepare
            const int processId = 123;
            using var session = environment.CreateAnalysisSession(processId);
            var analysisTask = session.Start();

            // Act
            session.Profiler.Start();
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ModuleLoaded = new Notify_ModuleLoaded()
                {
                    ModuleId = 456,
                    ModulePath = typeof(BasicProfilingEventTests).Assembly.Location
                },
                NotificationId = 2,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                TypeLoaded = new Notify_TypeLoaded()
                {
                    ModuleId = 456,
                    TypeToken = (uint)typeof(BasicProfilingEventTests).MetadataToken
                },
                NotificationId = 3,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                JITCompilationStarted = new Notify_JITCompilationStarted()
                {
                    ModuleId = 456,
                    TypeToken = (uint)typeof(BasicProfilingEventTests).MetadataToken,
                    FunctionToken = (uint)typeof(BasicProfilingEventTests)
                        .GetMethod(nameof(BasicProfilingEventTests_JITCompilationStarted))!.MetadataToken
                },
                NotificationId = 4,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 5,
                ProcessId = processId,
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

        [Fact]
        public async Task BasicProfilingEventTests_GarbageCollectionBeginsAndTerminates()
        {
            // Prepare
            const int processId = 123;
            using var session = environment.CreateAnalysisSession(processId);
            var analysisTask = session.Start();

            // Act
            session.Profiler.Start();
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                NotificationId = 1,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                RuntimeSuspendStarted = new Notify_RuntimeSuspendStarted()
                {
                    Reason = SUSPEND_REASON.Gc
                },
                NotificationId = 2,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                GarbageCollectionStarted = new Notify_GarbageCollectionStarted()
                {
                    GenerationsCollected = Google.Protobuf.ByteString.Empty,
                    GenerationSegmentBounds = Google.Protobuf.ByteString.Empty
                },
                NotificationId = 3,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                GarbageCollectionFinished = new Notify_GarbageCollectionFinished()
                {
                    GenerationSegmentBounds = Google.Protobuf.ByteString.Empty
                },
                NotificationId = 4,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                RuntimeSuspendFinished = new Notify_RuntimeSuspendFinished(),
                NotificationId = 5,
                ProcessId = processId,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 6,
                ProcessId = processId,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysisTask);
            Assert.Equal(4, session.Output.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), session.Output.First());
            Assert.Equal(nameof(IPlugin.GarbageCollectionStarted), session.Output.Skip(1).First());
            Assert.Equal(nameof(IPlugin.GarbageCollectionFinished), session.Output.Skip(2).First());
            Assert.Equal(nameof(IPlugin.AnalysisEnded), session.Output.Skip(3).First());
        }
    }
}