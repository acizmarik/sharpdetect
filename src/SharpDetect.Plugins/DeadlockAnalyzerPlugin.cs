using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services.Reporting;
using System.Collections.Concurrent;

namespace SharpDetect.Plugins
{
    [PluginExport("DeadlockAnalyzer", "1.0.0")]
    [PluginDiagnosticsCategories(new string[] { DiagnosticsCategory })]
    public class DeadlockAnalyzerPlugin : NopPlugin
    {
        public const string DiagnosticsCategory = "Deadlock";
        public const string DiagnosticsMessageFormat = "Affected threads: {0}";

        private readonly ConcurrentDictionary<UIntPtr, HashSet<IShadowObject>> ownedLocks;
        private readonly ConcurrentDictionary<UIntPtr, IShadowObject?> blockedObjects;
        private IReportingService reportingService;
        private ILogger<DeadlockAnalyzerPlugin> logger;

        public DeadlockAnalyzerPlugin()
        {
            this.ownedLocks = new ConcurrentDictionary<UIntPtr, HashSet<IShadowObject>>();
            this.blockedObjects = new ConcurrentDictionary<UIntPtr, IShadowObject?>();
            this.reportingService = null!;
            this.logger = null!;
        }

        public override void Initialize(IServiceProvider serviceProvider)
        {
            reportingService = serviceProvider.GetRequiredService<IReportingService>();
            logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<DeadlockAnalyzerPlugin>();
        }

        public override void LockAcquireAttempted(IShadowObject instance, EventInfo info)
        {
            blockedObjects[info.Thread.Id] = instance;
            EnsureNoDeadlocks(instance, info.Thread.Id, info.Runtime.ProcessId);
        }

        public override void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info)
        {
            if (isSuccess)
            {
                var collection = ownedLocks.GetOrAdd(info.Thread.Id, new HashSet<IShadowObject>());
                lock (collection)
                {
                    collection.Add(instance);
                }
            }

            blockedObjects[info.Thread.Id] = null;
        }

        public override void LockReleased(IShadowObject instance, EventInfo info)
        {
            var collection = ownedLocks[info.Thread.Id];
            lock (collection)
            {
                collection.Remove(instance);
            }
        }

        private void EnsureNoDeadlocks(IShadowObject attemptedLock, UIntPtr threadId, int processId)
        {
            var visited = new HashSet<UIntPtr>() { threadId };
            var chain = new List<UIntPtr>() { threadId };
            var obj = attemptedLock;

            while (obj is not null)
            {
                var nextThreadId = obj.SyncBlock?.LockOwnerId;
                if (!nextThreadId.HasValue)
                    return;

                obj = blockedObjects[nextThreadId.Value];
                if (visited.Contains(nextThreadId.Value))
                {
                    if (visited.Count == 1)
                    {
                        // Reentrancy
                        return;
                    }
                    else
                    {
                        // Detected deadlock
                        var threads = string.Join(", ", chain);
                        reportingService.Report(
                            new ErrorReport(
                                nameof(DeadlockAnalyzerPlugin),
                                DiagnosticsCategory,
                                DiagnosticsMessageFormat,
                                new[] { threads },
                                processId,
                                null));
                        return;
                    }
                }

                visited.Add(nextThreadId.Value);
                chain.Add(nextThreadId.Value);
            }
        }
    }
}
