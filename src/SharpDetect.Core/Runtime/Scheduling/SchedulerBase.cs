using SharpDetect.Common.Services;
using SharpDetect.Core.Runtime.Threads;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace SharpDetect.Core.Runtime.Scheduling
{
    internal abstract class SchedulerBase : IDisposable
    {
        public static readonly TimeSpan MaximumDelayBetweenHeartbeats = TimeSpan.FromSeconds(value: 3);
        public event Action? ProcessCrashed;
        public event Action? ProcessFinished;

        public int ProcessId { get; private set; }
        protected ImmutableDictionary<UIntPtr, ShadowThread> ThreadLookup;
        protected BlockingCollection<ShadowThread> ShadowThreadDestroyingQueue;
        protected readonly SchedulerEpochChangeSignaller EpochChangeSignaller;
        private readonly IDateTimeProvider dateTimeProvider;
        private volatile int virtualThreadsCounter;
        private DateTime lastHeartbeatTimeStamp;
        private Timer watchdog;
        private Task shadowThreadReaper;
        private bool isDisposed;

        public SchedulerBase(int processId, IDateTimeProvider dateTimeProvider)
        {
            this.ProcessId = processId;
            this.ThreadLookup = ImmutableDictionary.Create<UIntPtr, ShadowThread>();
            this.ShadowThreadDestroyingQueue = new BlockingCollection<ShadowThread>();
            this.EpochChangeSignaller = new SchedulerEpochChangeSignaller();
            this.dateTimeProvider = dateTimeProvider;
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

        protected void Schedule(UIntPtr threadId, ulong taskId, JobFlags flags, Task job)
        {
            var thread = ThreadLookup[threadId];
            thread.Execute(taskId, flags, job);
        }

        protected ShadowThread Register(UIntPtr threadId)
        {
            ShadowThread newThread;
            lock (EpochChangeSignaller)
                newThread = new ShadowThread(ProcessId, threadId, virtualThreadsCounter++, EpochChangeSignaller);

            ImmutableDictionary<UIntPtr, ShadowThread>? newLookup;
            ImmutableDictionary<UIntPtr, ShadowThread>? oldLookup;
            
            do
            {
                oldLookup = ThreadLookup;
                newLookup = oldLookup.Add(threadId, newThread);
            }
            while (Interlocked.CompareExchange(ref ThreadLookup, newLookup, oldLookup) != oldLookup);

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
                ShadowThreadDestroyingQueue.CompleteAdding();
                shadowThreadReaper.Wait();
                GC.SuppressFinalize(this);
            }
        }

        internal class SchedulerEpochChangeSignaller
        {
            public event Action<ulong>? EpochChanged;

            public ulong Epoch
            {
                get => epoch;
                set
                {
                    lock (this)
                    {
                        epoch = value;
                        EpochChanged?.Invoke(epoch);
                    }
                }
            }

            private ulong epoch;
        }
    }
}
