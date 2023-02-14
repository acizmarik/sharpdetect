using Microsoft.Extensions.Logging;
using SharpDetect.Common.Services;
using SharpDetect.Core.Runtime.Threads;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace SharpDetect.Core.Runtime.Scheduling
{
    internal abstract class SchedulerBase : IDisposable
    {
        public static readonly TimeSpan MaximumDelayBetweenHeartbeats = TimeSpan.FromSeconds(value: 30);
        public event Action? ProcessCrashed;
        public event Action? ProcessFinished;

        public int ProcessId { get; private set; }
        protected ImmutableDictionary<UIntPtr, ShadowThread> ThreadLookup;
        protected BlockingCollection<ShadowThread> ShadowThreadDestroyingQueue;
        protected readonly EpochSource EpochSource;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ILoggerFactory loggerFactory;
        private volatile int virtualThreadsCounter;
        private DateTime lastHeartbeatTimeStamp;
        private Timer watchdog;
        private Task shadowThreadReaper;
        private bool isDisposed;

        public SchedulerBase(int processId, IDateTimeProvider dateTimeProvider, ILoggerFactory loggerFactory)
        {
            this.ProcessId = processId;
            this.ThreadLookup = ImmutableDictionary.Create<UIntPtr, ShadowThread>();
            this.ShadowThreadDestroyingQueue = new BlockingCollection<ShadowThread>();
            this.EpochSource = new EpochSource();
            this.dateTimeProvider = dateTimeProvider;
            this.loggerFactory = loggerFactory;
            this.lastHeartbeatTimeStamp = dateTimeProvider.Now;
            this.watchdog = new Timer(
                callback: CheckWatchdog, state: null, 
                dueTime: TimeSpan.Zero, 
                period: MaximumDelayBetweenHeartbeats);
            this.shadowThreadReaper = new Task(() =>
            {
                foreach (var thread in ShadowThreadDestroyingQueue.GetConsumingEnumerable())
                    thread.Dispose();
            }, TaskCreationOptions.LongRunning);
            shadowThreadReaper.Start();
        }

        internal IEnumerable<ShadowThread> ShadowThreads
        {
            get => ThreadLookup.Values;
        }

        private void CheckWatchdog(object? _)
        {
            var currentTimeStamp = dateTimeProvider.Now;
            if (currentTimeStamp - lastHeartbeatTimeStamp > MaximumDelayBetweenHeartbeats)
            {
                watchdog.Dispose();
                ProcessCrashed?.Invoke();
            }
        }

        protected void Terminate()
            => ProcessFinished?.Invoke();

        protected void FeedWatchdog()
            => lastHeartbeatTimeStamp = dateTimeProvider.Now;

        protected void Schedule(UIntPtr threadId, ulong taskId, JobFlags flags, Action job)
        {
            // Note: there is a possibility that a notification might arrive during termination
            // However, in this case we should probably just discard it
            if (ThreadLookup.TryGetValue(threadId, out var thread))
                thread.Execute(taskId, flags, job);
        }

        protected ShadowThread Register(UIntPtr threadId)
        {
            if (ThreadLookup.TryGetValue(threadId, out var shadowThread))
                return shadowThread;

            ShadowThread newThread;
            newThread = new ShadowThread(ProcessId, threadId, virtualThreadsCounter++, loggerFactory, EpochSource);
            ImmutableDictionary<UIntPtr, ShadowThread>? newLookup;
            ImmutableDictionary<UIntPtr, ShadowThread>? oldLookup;
            
            do
            {
                oldLookup = ThreadLookup;
                newLookup = oldLookup.Add(threadId, newThread);
            }
            while (Interlocked.CompareExchange(ref ThreadLookup, newLookup, oldLookup) != oldLookup);

            newThread.Start();
            return newThread;
        }

        protected ShadowThread UnregisterThread(UIntPtr threadId)
        {
            var removedThread = ThreadLookup[threadId];
            ImmutableDictionary<UIntPtr, ShadowThread>? newLookup;
            ImmutableDictionary<UIntPtr, ShadowThread>? oldLookup;

            do
            {
                oldLookup = ThreadLookup;
                newLookup = oldLookup.Remove(threadId);
            }
            while (Interlocked.CompareExchange(ref ThreadLookup, newLookup, oldLookup) != oldLookup);

            // Queue for destroying
            ShadowThreadDestroyingQueue.Add(removedThread);

            return removedThread;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                watchdog.Dispose();
                foreach (var (id, _) in ThreadLookup)
                    UnregisterThread(id);
                ShadowThreadDestroyingQueue.CompleteAdding();

                try
                {
                    shadowThreadReaper.Wait();
                }
                catch (ThreadInterruptedException)
                {
                    // It is necessary to interrupt thread because it might be blocked
                    // This is a normal state
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}
