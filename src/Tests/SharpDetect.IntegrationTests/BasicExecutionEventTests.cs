using dnlib.DotNet;
using dnlib.DotNet.MD;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Plugins;
using Xunit;

namespace SharpDetect.IntegrationTests
{
    [Collection("Sequential")]
    public class BasicExecutionEventTests : IClassFixture<EnvironmentFixture>
    {
        private readonly EnvironmentFixture environment;

        public BasicExecutionEventTests(EnvironmentFixture fixture)
        {
            environment = fixture;
        }

        [Fact]
        public async Task BasicExecutionEventTests_LockAcquiredAndReleased()
        {
            // Prepar
            using var session = environment.CreateAnalysisSession();
            var analysisTask = session.Start();
            var typeTokenAcquire = (uint)typeof(Monitor).MetadataToken;
            var typeTokenRelease = (uint)typeof(Monitor).MetadataToken;
            var methodTokenAcquire = (uint)typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Enter) && m.GetParameters().Length == 1).MetadataToken;
            var methodTokenRelease = (uint)typeof(Monitor).GetMethod(nameof(Monitor.Exit))!.MetadataToken;
            var argValues = new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 /* UIntPtr */};
            var argOffsets = new byte[] { 8, 0 /* Value length */, 0, 0 /* Index */};

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
                    ModulePath = typeof(object).Assembly.Location
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
                    TypeToken = typeTokenAcquire
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
                    TypeToken = typeTokenAcquire,
                    FunctionToken = methodTokenAcquire
                },
                NotificationId = 4,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                JITCompilationStarted = new Notify_JITCompilationStarted()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenRelease,
                    FunctionToken = methodTokenRelease
                },
                NotificationId = 5,
                ProcessId = 123,
                ThreadId = 0,
            });
            // Monitor::Enter(...)
            session.Profiler.Send(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenAcquire,
                    FunctionToken = methodTokenAcquire,
                    ArgumentValues = Google.Protobuf.ByteString.CopyFrom(argValues),
                    ArgumentOffsets = Google.Protobuf.ByteString.CopyFrom(argOffsets)
                },
                NotificationId = 6,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenAcquire,
                    FunctionToken = methodTokenAcquire
                },
                NotificationId = 7,
                ProcessId = 123,
                ThreadId = 0,
            });
            // Monitor::Exit(...)
            session.Profiler.Send(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenRelease,
                    FunctionToken = methodTokenRelease,
                    ArgumentValues = Google.Protobuf.ByteString.CopyFrom(argValues),
                    ArgumentOffsets = Google.Protobuf.ByteString.CopyFrom(argOffsets)
                },
                NotificationId = 8,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenRelease,
                    FunctionToken = methodTokenRelease
                },
                NotificationId = 9,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 10,
                ProcessId = 123,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysisTask);
            Assert.Equal(13, session.Output.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), session.Output.First());
            Assert.Equal(nameof(IPlugin.ModuleLoaded), session.Output.Skip(1).First());
            Assert.Equal(nameof(IPlugin.TypeLoaded), session.Output.Skip(2).First());
            Assert.Equal(nameof(IPlugin.JITCompilationStarted), session.Output.Skip(3).First());
            Assert.Equal(nameof(IPlugin.JITCompilationStarted), session.Output.Skip(4).First());

            // Monitor::Enter(...)
            Assert.Equal(nameof(IPlugin.LockAcquireAttempted), session.Output.Skip(5).First());
            Assert.Equal(nameof(IPlugin.MethodCalled), session.Output.Skip(6).First());
            Assert.Equal(nameof(IPlugin.LockAcquireReturned), session.Output.Skip(7).First());
            Assert.Equal(nameof(IPlugin.MethodReturned), session.Output.Skip(8).First());

            // Monitor::Exit(...)
            Assert.Equal(nameof(IPlugin.MethodCalled), session.Output.Skip(9).First());
            Assert.Equal(nameof(IPlugin.LockReleased), session.Output.Skip(10).First());
            Assert.Equal(nameof(IPlugin.MethodReturned), session.Output.Skip(11).First());

            Assert.Equal(nameof(IPlugin.AnalysisEnded), session.Output.Skip(12).First());
        }

        [Fact]
        public async Task BasicExecutionEventTests_LockAcquiredAndReleased_WithWrappedExterns()
        {
            // Prepar
            using var session = environment.CreateAnalysisSession();
            var analysisTask = session.Start();
            var typeTokenAcquire = (uint)typeof(Monitor).MetadataToken;
            var typeTokenRelease = (uint)typeof(Monitor).MetadataToken;
            var methodTokenAcquire = (uint)typeof(Monitor).GetMethods().Single(m => m.Name == nameof(Monitor.Enter) && m.GetParameters().Length == 1).MetadataToken;
            var methodTokenRelease = (uint)typeof(Monitor).GetMethod(nameof(Monitor.Exit))!.MetadataToken;
            var methodTokenAcquireWrapper = new MDToken(Table.Method, 0x00FF_FFFF).Raw;
            var methodTokenReleaseWrapper = new MDToken(Table.Method, 0x00FF_FFFE).Raw;
            var argValues = new byte[] { 0, 0, 0, 0, 0, 0, 0, 123 /* UIntPtr */};
            var argOffsets = new byte[] { 8, 0 /* Value length */, 0, 0 /* Index */};

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
                    ModulePath = typeof(object).Assembly.Location
                },
                NotificationId = 2,
                ProcessId = 123,
                ThreadId = 0,
            });
            // Injecting method wrappers
            session.Profiler.Send(new NotifyMessage()
            {
                MethodWrapperInjected = new Notify_MethodWrapperInjected()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenAcquire,
                    OriginalFunctionToken = methodTokenAcquire,
                    WrapperFunctionToken = methodTokenAcquireWrapper
                },
                NotificationId = 3,
                ProcessId = 123,
                ThreadId = 0
            });
            session.Profiler.Send(new NotifyMessage()
            {
                MethodWrapperInjected = new Notify_MethodWrapperInjected()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenAcquire,
                    OriginalFunctionToken = methodTokenRelease,
                    WrapperFunctionToken = methodTokenReleaseWrapper
                },
                NotificationId = 4,
                ProcessId = 123,
                ThreadId = 0
            });
            session.Profiler.Send(new NotifyMessage()
            {
                WrapperMethodReferenced = new Notify_WrapperMethodReferenced()
                {
                    DefModuleId = 456,
                    RefModuleId = 456,
                    DefTypeToken = typeTokenAcquire,
                    RefTypeToken = typeTokenAcquire,
                    DefFunctionToken = methodTokenAcquire,
                    RefFunctionToken = methodTokenAcquireWrapper
                },
                NotificationId = 5,
                ProcessId = 123,
                ThreadId = 0
            });
            session.Profiler.Send(new NotifyMessage()
            {
                WrapperMethodReferenced = new Notify_WrapperMethodReferenced()
                {
                    DefModuleId = 456,
                    RefModuleId = 456,
                    DefTypeToken = typeTokenRelease,
                    RefTypeToken = typeTokenRelease,
                    DefFunctionToken = methodTokenRelease,
                    RefFunctionToken = methodTokenReleaseWrapper
                },
                NotificationId = 6,
                ProcessId = 123,
                ThreadId = 0
            });
            session.Profiler.Send(new NotifyMessage()
            {
                TypeLoaded = new Notify_TypeLoaded()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenAcquire
                },
                NotificationId = 7,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenAcquire,
                    FunctionToken = methodTokenAcquireWrapper,
                    ArgumentValues = Google.Protobuf.ByteString.CopyFrom(argValues),
                    ArgumentOffsets = Google.Protobuf.ByteString.CopyFrom(argOffsets)
                },
                NotificationId = 8,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenAcquire,
                    FunctionToken = methodTokenAcquireWrapper
                },
                NotificationId = 9,
                ProcessId = 123,
                ThreadId = 0,
            });
            // Monitor::Exit(...)
            session.Profiler.Send(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenRelease,
                    FunctionToken = methodTokenReleaseWrapper,
                    ArgumentValues = Google.Protobuf.ByteString.CopyFrom(argValues),
                    ArgumentOffsets = Google.Protobuf.ByteString.CopyFrom(argOffsets)
                },
                NotificationId = 10,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = 456,
                    TypeToken = typeTokenRelease,
                    FunctionToken = methodTokenReleaseWrapper
                },
                NotificationId = 11,
                ProcessId = 123,
                ThreadId = 0,
            });
            session.Profiler.Send(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                NotificationId = 12,
                ProcessId = 123,
                ThreadId = 0
            });

            // Assert
            Assert.True(await analysisTask);
            Assert.Equal(11, session.Output.Count);
            Assert.Equal(nameof(IPlugin.AnalysisStarted), session.Output.First());
            Assert.Equal(nameof(IPlugin.ModuleLoaded), session.Output.Skip(1).First());
            Assert.Equal(nameof(IPlugin.TypeLoaded), session.Output.Skip(2).First());

            // Monitor::Enter(...)
            Assert.Equal(nameof(IPlugin.LockAcquireAttempted), session.Output.Skip(3).First());
            Assert.Equal(nameof(IPlugin.MethodCalled), session.Output.Skip(4).First());
            Assert.Equal(nameof(IPlugin.LockAcquireReturned), session.Output.Skip(5).First());
            Assert.Equal(nameof(IPlugin.MethodReturned), session.Output.Skip(6).First());

            // Monitor::Exit(...)
            Assert.Equal(nameof(IPlugin.MethodCalled), session.Output.Skip(7).First());
            Assert.Equal(nameof(IPlugin.LockReleased), session.Output.Skip(8).First());
            Assert.Equal(nameof(IPlugin.MethodReturned), session.Output.Skip(9).First());

            Assert.Equal(nameof(IPlugin.AnalysisEnded), session.Output.Skip(10).First());
        }
    }
}
