using Microsoft.Extensions.DependencyInjection;
using NetMQ;
using SharpDetect.Common;
using SharpDetect.Common.Services;
using SharpDetect.Console;
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
