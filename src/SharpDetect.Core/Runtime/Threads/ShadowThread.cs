using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Core.Runtime.Scheduling;
using System.Threading.Channels;

namespace SharpDetect.Core.Runtime.Threads
{
    internal class ShadowThread : IShadowThread, IDisposable
    {
        public UIntPtr Id { get; private set; }
        public int VirtualId { get; private set; }
        public int ProcessId { get; private set; }
        public Epoch Epoch { get; private set; }
        public string DisplayName { get; private set; }
        public ShadowThreadState State { get { return state; } }
        public OperationContext OperationContext { get; private set; }

        internal readonly ManualResetEvent SuspensionSignal;
        internal readonly ManualResetEvent GarbageCollectionSignal;
        internal readonly ManualResetEvent RunningSignal;

        private readonly ILogger<ShadowThread> logger;
        private readonly Channel<(ulong Id, Action Job, JobFlags Flags)> highPriorityQueue;
        private readonly Channel<(ulong Id, Action Job, JobFlags Flags)> lowPriorityQueue;
        private readonly EpochSource epochSource;
        private readonly Stack<StackFrame> callstack;
        private readonly Thread workerThread;
        private volatile ShadowThreadState state;
        private bool isDisposed;

        public ShadowThread(int processId, UIntPtr threadId, int virtualThreadId, ILoggerFactory loggerFactory, EpochSource epochSource)
        {
            ProcessId = processId;
            Id = threadId;
            VirtualId = virtualThreadId;
            DisplayName = $"{nameof(ShadowThread)}-{virtualThreadId}";
            OperationContext = new OperationContext();
            state = ShadowThreadState.Running;
            callstack = new Stack<StackFrame>();

            this.logger = loggerFactory.CreateLogger<ShadowThread>();
            this.epochSource = epochSource;
            SuspensionSignal = new ManualResetEvent(false);
            GarbageCollectionSignal = new ManualResetEvent(false);
            RunningSignal = new ManualResetEvent(false);

            highPriorityQueue = Channel.CreateUnbounded<(ulong Id, Action Job, JobFlags Flags)>();
            lowPriorityQueue = Channel.CreateUnbounded<(ulong Id, Action Job, JobFlags Flags)>();
            workerThread = new Thread(WorkerThreadLoop) { Name = DisplayName };
            workerThread.Name = DisplayName;
        }

        private void WorkerThreadLoop()
        {
            try
            {
                EnterState(ShadowThreadState.Running);
                var tasksArray = new Task<bool>[2];
                var readers = new ChannelReader<(ulong, Action, JobFlags)>[2]
                {
                    highPriorityQueue.Reader,
                    lowPriorityQueue.Reader
                };

                while (true)
                {
                    var index = 0;
                    var success = true;
                    var jobInfo = default((ulong Id, Action Job, JobFlags Flags));
                    for (index = 0; index < readers.Length; index++)
                    {
                        // Check if there is something available in a queue
                        success = readers[index].TryRead(out jobInfo);
                        if (success)
                            break;
                    }

                    if (!success)
                    {
                        // Wait until something becomes available in a queue
                        for (index = 0; index < readers.Length; index++)
                            tasksArray[index] = readers[index].WaitToReadAsync().AsTask();
                        index = Task.WaitAny(tasksArray);
                        success = tasksArray[index].Result;
                        jobInfo = success ? readers[index].ReadAsync().AsTask().Result : jobInfo;

                        if (!success)
                        {
                            // Queue is completed - terminating thread
                            break;
                        }
                    }

                    var (id, job, flags) = jobInfo;
                    while (epochSource.CurrentEpoch.Value < Epoch.Value && !(flags.HasFlag(JobFlags.OverrideSuspend) || flags.HasFlag(JobFlags.Poison)))
                    {
                        // Wait for next epoch
                        epochSource.WaitForNextEpoch(this);
                    }

                    // Execute the action
                    job!.Invoke();

                    if (flags.HasFlag(JobFlags.Poison))
                    {
                        // Received poison pill - terminating thread
                        break;
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                // Do nothing - this is a way to terminate blocked threads
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Execution of shadow thread failed due to an unhandled exception.");
            }
        }

        public void Start()
            => workerThread.Start();

        public void Execute(ulong notificationId, JobFlags flags, Action job, bool highPriority = true)
            => (highPriority ? highPriorityQueue.Writer : lowPriorityQueue.Writer).TryWrite((notificationId, job, flags));

        public void SetName(string name)
            => DisplayName = name;

        public StackFrame PeekCallstack()
            => callstack.Peek();

        public int GetCallstackDepth()
            => callstack.Count;

        public void PushCallStack(FunctionInfo info, MethodInterpretation interpretation, object? args = null)
            => callstack.Push(new(info, interpretation, args));

        public void EnterState(ShadowThreadState newState)
        {
            state = newState;
            switch (newState)
            {
                case ShadowThreadState.Running:
                    SuspensionSignal.Reset();
                    GarbageCollectionSignal.Reset();
                    RunningSignal.Set();
                    break;
                case ShadowThreadState.Suspended:
                    RunningSignal.Reset();
                    GarbageCollectionSignal.Reset();
                    SuspensionSignal.Set();
                    break;
                case ShadowThreadState.GarbageCollecting:
                    RunningSignal.Reset();
                    SuspensionSignal.Reset();
                    GarbageCollectionSignal.Set();
                    break;
            }
        }

        public StackFrame PopCallStack()
            => callstack.Pop();

        public void EnterNewEpoch(ulong? newValue = null)
            => Epoch = new(newValue ?? Epoch.Value + 1);

        private void ExecuteJobSynchronously(Action job)
        {
            try
            {
                job();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[TID={tid}] An error occurred while processing a shadow task.", DisplayName);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                highPriorityQueue.Writer.Complete();
                lowPriorityQueue.Writer.Complete();
                if (workerThread.ThreadState != ThreadState.Unstarted)
                {
                    // Wait for some time
                    // Then issue interrupts (thread could be blocked)
                    while (!workerThread.Join(timeout: TimeSpan.FromSeconds(3)))
                        workerThread.Interrupt();
                }

                GC.SuppressFinalize(this);
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
