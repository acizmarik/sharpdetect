using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Runtime;

namespace SharpDetect.Core
{
    internal class Analysis : IAnalysis
    {
        private readonly IConfiguration configuration;
        private readonly RuntimeEventsHub runtimeEventsHub;
        private readonly INotificationsConsumer notificationsConsumer;
        private readonly IRequestsProducer requestsProducer;
        private readonly IProfilingMessageHub profilingMessageHub;
        private readonly IRewritingMessageHub rewritingMessageHub;
        private readonly IExecutingMessageHub executingMessageHub;
        private readonly IModuleBindContext moduleBindContext;
        private readonly IMetadataContext metadataContext;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IServiceProvider serviceProvider;
        private readonly IInstrumentor instrumentor;
        private readonly IPluginsManager pluginsManager;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<Analysis> logger;

        public Analysis(
            IConfiguration configuration, 
            RuntimeEventsHub runtimeEventsHub,
            IProfilingMessageHub profilingMessageHub,
            IRewritingMessageHub rewritingMessageHub,
            IExecutingMessageHub executingMessageHub,
            IModuleBindContext moduleBindContext,
            IMetadataContext metadataContext,
            INotificationsConsumer notificationsConsumer,
            IRequestsProducer requestsProducer,
            IInstrumentor instrumentor,
            IPluginsManager pluginsManager,
            IDateTimeProvider dateTimeProvider,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            this.configuration = configuration;
            this.runtimeEventsHub = runtimeEventsHub;
            this.profilingMessageHub = profilingMessageHub;
            this.rewritingMessageHub = rewritingMessageHub;
            this.executingMessageHub = executingMessageHub;
            this.moduleBindContext = moduleBindContext;
            this.metadataContext = metadataContext;
            this.notificationsConsumer = notificationsConsumer;
            this.requestsProducer = requestsProducer;
            this.instrumentor = instrumentor;
            this.pluginsManager = pluginsManager;
            this.dateTimeProvider = dateTimeProvider;
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
            
            logger = loggerFactory.CreateLogger<Analysis>();
        }

        public async Task<bool> ExecuteAsync(CancellationToken ct)
        {
            DateTime? start = default, stop = default;

            var execution = new ShadowExecution(
                runtimeEventsHub, 
                profilingMessageHub, 
                rewritingMessageHub, 
                executingMessageHub, 
                moduleBindContext,
                metadataContext,
                dateTimeProvider, 
                loggerFactory);

            try
            {
                await pluginsManager.LoadPluginsAsync(ct).ConfigureAwait(false);
                using var pluginsProxy = new PluginsProxy(configuration, serviceProvider, pluginsManager, runtimeEventsHub);
                pluginsProxy.Initialize();
                logger.LogDebug("[{class}] Analysis started.", nameof(Analysis));
                requestsProducer.Start();
                notificationsConsumer.Start();
                start = dateTimeProvider.Now;

                // Wait until execution completes
                await execution.GetAwaitableTaskAsync().ConfigureAwait(false);
                return execution.ProcessesExitedWithErrorCodeCount == 0;
            }
            finally
            {
                notificationsConsumer.Stop();
                requestsProducer.Stop();
                stop = dateTimeProvider.Now;
                logger.LogDebug("[{class}] Analysis ended.", nameof(Analysis));
                DumpStatistics(start, stop);
            }
        }

        private void DumpStatistics(DateTime? start, DateTime? stop)
        {
            var duration = (start.HasValue) ? stop - start : TimeSpan.Zero;

            logger.LogInformation(
                "[{class}] Execution information: {lf}" +
                "- Duration: {duration}{lf}" +
                "- Number of instrumented methods: {instrumented}{lf}" +
                "- Number of injected method hooks: {hooks}{lf}" +
                "- Number of injected method wrappers: {wrappers}",
                /* line 1 args */ nameof(Analysis), Environment.NewLine, 
                /* line 2 args */ duration, Environment.NewLine,
                /* line 3 args */ instrumentor.InstrumentedMethodsCount, Environment.NewLine,
                /* line 4 args */ instrumentor.InjectedMethodHooksCount, Environment.NewLine,
                /* line 5 args */ instrumentor.InjectedMethodWrappersCount);
        }
    }
}