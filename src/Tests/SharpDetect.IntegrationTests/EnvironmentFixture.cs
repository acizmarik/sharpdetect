using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common;
using SharpDetect.Common.Services;
using SharpDetect.Console;
using SharpDetect.Core;
using SharpDetect.IntegrationTests.Mocks;
using System.Collections.Concurrent;

namespace SharpDetect.IntegrationTests
{
    public class EnvironmentFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; }
        private bool isDisposed;

        public EnvironmentFixture()
        {
            var configuration = Program.CreateConfiguration();
            configuration[Constants.Configuration.PluginsChain] = "Internal-TestPlugin";
            configuration[Constants.Configuration.PluginsRootFolder] = Directory.GetCurrentDirectory();
            var services = new ServiceCollection();
            Program.ConfigureCommonServices(services, configuration);
            services.AddSingleton<IDateTimeProvider, MockDateTimeProvider>();
            services.AddScoped<BlockingCollection<string>>();
            ServiceProvider = services.BuildServiceProvider();
        }

        public AnalysisSession CreateAnalysisSession()
        {
            var scope = ServiceProvider.CreateScope();
            var provider = scope.ServiceProvider;
            var configuration = provider.GetRequiredService<IConfiguration>();

            var profiler = new MockProfiler(configuration);
            var analysis = provider.GetRequiredService<IAnalysis>();
            var sink = provider.GetRequiredService<BlockingCollection<string>>();

            return new(scope, analysis, profiler, sink);
        }

        public record AnalysisSession(IServiceScope Scope, IAnalysis Analyzer, MockProfiler Profiler, BlockingCollection<string> Output) : IDisposable
        {
            private bool isDisposed;

            public Task<bool> Start()
            {
                return Analyzer.ExecuteAsync(CancellationToken.None);
            }

            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    Output.CompleteAdding();
                    Profiler.Dispose();
                    Scope.Dispose();
                }
            }
        }

        class MockDateTimeProvider : IDateTimeProvider
        {
            public DateTime CurrentTime { get; set; }

            public DateTime Now => CurrentTime;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
