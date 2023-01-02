using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Common.Services.Reporting;
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
        private readonly IProfilingClient profilingClient;
        private readonly IModuleBindContext moduleBindContext;
        private readonly IMetadataContext metadataContext;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly IServiceProvider serviceProvider;
        private readonly IInstrumentor instrumentor;
        private readonly IPluginsManager pluginsManager;
        private readonly IMethodDescriptorRegistry methodRegistry;
        private readonly IReportsReaderProvider reportsReaderProvider;
        private readonly IReportingServiceController reportingController;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<Analysis> logger;

        public Analysis(
            IConfiguration configuration,
            RuntimeEventsHub runtimeEventsHub,
            IProfilingMessageHub profilingMessageHub,
            IRewritingMessageHub rewritingMessageHub,
            IExecutingMessageHub executingMessageHub,
            IProfilingClient profilingClient,
            IModuleBindContext moduleBindContext,
            IMetadataContext metadataContext,
            INotificationsConsumer notificationsConsumer,
            IRequestsProducer requestsProducer,
            IInstrumentor instrumentor,
            IPluginsManager pluginsManager,
            IMethodDescriptorRegistry methodRegistry,
            IReportsReaderProvider reportsReaderProvider,
            IReportingServiceController reportingController,
            IDateTimeProvider dateTimeProvider,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            this.configuration = configuration;
            this.runtimeEventsHub = runtimeEventsHub;
            this.profilingMessageHub = profilingMessageHub;
            this.rewritingMessageHub = rewritingMessageHub;
            this.executingMessageHub = executingMessageHub;
            this.profilingClient = profilingClient;
            this.moduleBindContext = moduleBindContext;
            this.metadataContext = metadataContext;
            this.notificationsConsumer = notificationsConsumer;
            this.requestsProducer = requestsProducer;
            this.instrumentor = instrumentor;
            this.pluginsManager = pluginsManager;
            this.reportsReaderProvider = reportsReaderProvider;
            this.methodRegistry = methodRegistry;
            this.reportingController = reportingController;
            this.dateTimeProvider = dateTimeProvider;
            this.serviceProvider = serviceProvider;
            this.loggerFactory = loggerFactory;
            
            logger = loggerFactory.CreateLogger<Analysis>();
        }

        public Task<bool> ExecuteAnalysisAndTargetAsync(CancellationToken ct)
            => ExecuteAsync(withTargetProgram: true, ct);

        public Task<bool> ExecuteOnlyAnalysisAsync(CancellationToken ct)
            => ExecuteAsync(withTargetProgram: false, ct);

        private async Task<bool> ExecuteAsync(bool withTargetProgram, CancellationToken ct)
        {
            DateTime? start = default, stop = default;

            using var execution = new ShadowExecution(
                runtimeEventsHub,
                profilingMessageHub,
                rewritingMessageHub,
                executingMessageHub,
                profilingClient,
                moduleBindContext,
                metadataContext,
                methodRegistry,
                dateTimeProvider,
                loggerFactory);

            try
            {
                await pluginsManager.LoadPluginsAsync(ct).ConfigureAwait(false);
                using var pluginsProxy = new PluginsProxy(configuration, serviceProvider, pluginsManager, runtimeEventsHub, loggerFactory);
                pluginsProxy.Initialize();
                logger.LogDebug("[{class}] Analysis started.", nameof(Analysis));
                requestsProducer.Start();
                notificationsConsumer.Start();
                start = dateTimeProvider.Now;

                if (withTargetProgram)
                {
                    var target = new Target(configuration);
                    logger.LogDebug("[{class}] Target program starting...", nameof(Analysis));
                    var result = await target.ExecuteAsync(ct).ConfigureAwait(false);
                    logger.LogDebug("[{class}] Target program returned {val}", nameof(Analysis), result.ExitCode);
                }

                await execution.GetAwaitableTaskAsync().ConfigureAwait(false);
                return execution.ProcessesExitedWithErrorCodeCount == 0;
            }
            finally
            {
                notificationsConsumer.Stop();
                requestsProducer.Stop();
                stop = dateTimeProvider.Now;
                reportingController.Complete();
                logger.LogDebug("[{class}] Analysis ended.", nameof(Analysis));
            }
        }
    }
}