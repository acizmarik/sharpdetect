using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common;
using SharpDetect.Common.Services;
using SharpDetect.Console;
using SharpDetect.Core;

namespace SharpDetect.E2ETests.Utilities
{
    public record class AnalysisSession(
        string TargetAssembly,
        KeyValuePair<string, string>[] CustomConfig) : IAsyncDisposable
    {
        public CancellationToken CancellationToken { get; private set; }
        private CancellationTokenSource? ctSource;
        private ServiceProvider? serviceProvider;
        private bool isDisposed;

        public Task<bool> Start()
        {
            var configuration = Program.CreateConfiguration();
            configuration[Constants.Configuration.TargetAssembly] = TargetAssembly;
            foreach (var (key, value) in CustomConfig)
                configuration[key] = value;
            var services = new ServiceCollection();
            Program.ConfigureCommonServices(services, configuration);
            services.AddSingleton<IDateTimeProvider, UtcDateTimeProvider>();
            serviceProvider = services.BuildServiceProvider();
            ctSource = new CancellationTokenSource();
            CancellationToken = ctSource.Token;

            var analysis = serviceProvider.GetRequiredService<IAnalysis>();
            return analysis.ExecuteAnalysisAndTargetAsync(ctSource.Token);
        }

        public TService GetRequiredService<TService>()
            where TService : class
        {
            if (serviceProvider == null)
                throw new InvalidOperationException("Create analysis session first.");

            return serviceProvider.GetRequiredService<TService>();
        }

        public async ValueTask DisposeAsync()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                ctSource?.Dispose();
                serviceProvider?.Dispose();
                GC.SuppressFinalize(this);
            }

            await Task.CompletedTask;
        }
    }
}
