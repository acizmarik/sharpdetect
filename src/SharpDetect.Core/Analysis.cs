using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Common.SourceLinks;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Runtime;
using System.Text;

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

        public Task<bool> ExecuteAnalysisAndTargetAsync(bool dumpStatistics, CancellationToken ct)
            => ExecuteAsync(withTargetProgram: true, dumpStatistics, ct);

        public Task<bool> ExecuteOnlyAnalysisAsync(bool dumpStatistics, CancellationToken ct)
            => ExecuteAsync(withTargetProgram: false, dumpStatistics, ct);

        private async Task<bool> ExecuteAsync(bool withTargetProgram, bool dumpStatistics, CancellationToken ct)
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
                var wholeExecution = execution.GetAwaitableTaskAsync();

                if (withTargetProgram)
                {
                    var target = new Target(configuration);
                    logger.LogDebug("[{class}] Target program starting...", nameof(Analysis));
                    wholeExecution = target.ExecuteAsync(ct).ContinueWith(_ => execution);
                }

                await wholeExecution.ConfigureAwait(false);
                return execution.ProcessesExitedWithErrorCodeCount == 0;
            }
            finally
            {
                notificationsConsumer.Stop();
                requestsProducer.Stop();
                stop = dateTimeProvider.Now;
                reportingController.Complete();
                logger.LogDebug("[{class}] Analysis ended.", nameof(Analysis));
                if (dumpStatistics)
                    await DumpStatisticsAsync(start, stop);
            }
        }

        private async Task DumpStatisticsAsync(DateTime? start, DateTime? stop)
        {
            var duration = (start.HasValue) ? stop - start : TimeSpan.Zero;
            var reader = reportsReaderProvider.GetReportsReader();

            logger.LogInformation(
                "[{class}] Execution information: {lf}" +
                "- Duration: {duration}{lf}" +
                "- Number of instrumented methods: {instrumented}{lf}" +
                "- Number of injected method hooks: {hooks}{lf}" +
                "- Number of injected method wrappers: {wrappers}{lf}" +
                "- Number of reports: {count}{lf}",
                /* line 1 args */ nameof(Analysis), Environment.NewLine, 
                /* line 2 args */ duration, Environment.NewLine,
                /* line 3 args */ instrumentor.InstrumentedMethodsCount, Environment.NewLine,
                /* line 4 args */ instrumentor.InjectedMethodHooksCount, Environment.NewLine,
                /* line 5 args */ instrumentor.InjectedMethodWrappersCount, Environment.NewLine,
                /* line 6 args */ reader.Count, Environment.NewLine);

            var reports = new Dictionary<(Type Type, string reporter, string Category, string Description), (int Count, HashSet<SourceLink> SourceLinks)>();
            await foreach (var report in reader.ReadAllAsync())
            {
                var key = (report.GetType(), report.Reporter, report.Category, report.Description);
                if (!reports.ContainsKey(key))
                    reports[key] = (0, new HashSet<SourceLink>(report.SourceLinks ?? Enumerable.Empty<SourceLink>()));

                var (count, sourceLinks) = reports[key];
                foreach (var link in report.SourceLinks ?? Enumerable.Empty<SourceLink>())
                    sourceLinks.Add(link);

                reports[key] = (count + 1, sourceLinks);
            }

            foreach (var ((reportType, reporter, category, description), (count, sourceLinks)) in reports.OrderByDescending(e => e.Value.Count))
            {
                var messageBuilder = new StringBuilder();
                var argumentsBuilder = new List<object>();
                messageBuilder.Append("[{reporter}][{category}] {description} reported {n}-times");
                argumentsBuilder.AddRange(new object[] { reporter, category, description, count });

                if (sourceLinks.Count > 0)
                {
                    foreach (var link in sourceLinks)
                    {
                        messageBuilder.Append(Environment.NewLine);
                        messageBuilder.Append("\tat {method} on offset {instruction}");
                        argumentsBuilder.Add(link.Method);
                        argumentsBuilder.Add(link.Instruction);
                    }
                }

                logger.LogWarning(messageBuilder.ToString(), argumentsBuilder.ToArray());
            }
        }
    }
}