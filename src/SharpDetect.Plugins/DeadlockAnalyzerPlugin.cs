using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Plugins.Metadata;
using SharpDetect.Common.Runtime;
using System.Collections.Concurrent;

namespace SharpDetect.Plugins
{
    [PluginExport("DeadlockAnalyzer", "1.0.0")]
    public class DeadlockAnalyzerPlugin : NopPlugin
    {
        private readonly ConcurrentDictionary<UIntPtr, HashSet<IShadowObject>> ownedLocks;
        private readonly ConcurrentDictionary<UIntPtr, IShadowObject?> blockedObjects;
        private ILogger<DeadlockAnalyzerPlugin> logger;

        public DeadlockAnalyzerPlugin()
        {
            this.ownedLocks = new ConcurrentDictionary<UIntPtr, HashSet<IShadowObject>>();
            this.blockedObjects = new ConcurrentDictionary<UIntPtr, IShadowObject?>();
            this.logger = null!;
        }

        public override void Initialize(IServiceProvider serviceProvider)
        {
            logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<DeadlockAnalyzerPlugin>();
        }

        public override void LockAcquireAttempted(IShadowObject instance, EventInfo info)
        {
            blockedObjects[info.ThreadId] = instance;
            EnsureNoDeadlocks(instance, info.ThreadId, info.ProcessId);
        }

        public override void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info)
        {
            if (isSuccess)
            {
                var collection = ownedLocks.GetOrAdd(info.ThreadId, new HashSet<IShadowObject>());
                lock (collection)
                {
                    collection.Add(instance);
                }
            }

            blockedObjects[info.ThreadId] = null;
        }

        public override void LockReleased(IShadowObject instance, EventInfo info)
        {
            var collection = ownedLocks[info.ThreadId];
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
                        logger.LogError("[PID={pid}][{plugin}] Detected deadlock between the following threads: {threads}!", 
                            processId, nameof(DeadlockAnalyzerPlugin), threads);
                        
                        return;
                    }
                }

                visited.Add(nextThreadId.Value);
                chain.Add(nextThreadId.Value);
            }
        }
    }
}
