﻿using SharpDetect.Common;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Core.Runtime.Scheduling;
using System.Collections.Concurrent;

namespace SharpDetect.Core.Runtime.Threads
{
    internal class ShadowThread : IShadowThread, IDisposable
    {
        public UIntPtr Id { get; private set; }
        public int ProcessId { get; private set; }
        public ulong Epoch { get; private set; }
        public string DisplayName { get; private set; }
        public ShadowThreadState State { get; private set; }
        public OperationContext OperationContext { get; private set; }

        private ulong schedulerEpoch;
        private readonly SchedulerBase.SchedulerEpochChangeSignaller schedulerEpochChangeSignaler;
        private readonly BlockingCollection<(Task Job, ulong NotificationId, JobFlags Flags)> jobs;
        private readonly Queue<(Task Job, ulong NotificationId)> waitingJobs;
        private readonly Stack<StackFrame> callstack;
        private readonly Thread workerThread;
        private bool isDisposed;

        public ShadowThread(int processId, UIntPtr threadId, int virtualThreadId, SchedulerBase.SchedulerEpochChangeSignaller schedulerEpochChangeSignaler)
        {
            ProcessId = processId;
            Id = threadId;
            Epoch = schedulerEpochChangeSignaler.Epoch;
            DisplayName = $"{nameof(ShadowThread)}-{virtualThreadId}";
            State = ShadowThreadState.Running;
            OperationContext = new OperationContext();
            callstack = new Stack<StackFrame>();

            this.schedulerEpochChangeSignaler = schedulerEpochChangeSignaler;
            this.schedulerEpochChangeSignaler.EpochChanged += OnSchedulerEpochChanged;
            this.schedulerEpoch = schedulerEpochChangeSignaler.Epoch;
            
            jobs = new BlockingCollection<(Task Job, ulong NotificationId, JobFlags Flags)>();
            waitingJobs = new Queue<(Task Job, ulong NotificationId)>();
            workerThread = new Thread(WorkerThreadLoop) { Name = DisplayName };
        }

        private void OnSchedulerEpochChanged(ulong newEpoch)
        {
            schedulerEpoch = newEpoch;
        }

        private void WorkerThreadLoop()
        {
            try
            {
                // Execute all received jobs until terminated
                foreach (var (job, id, flags) in jobs.GetConsumingEnumerable())
                {
                    if (schedulerEpoch < Epoch && (!flags.HasFlag(JobFlags.OverrideSuspend)))
                    {
                        // This thread needs to wait for other thread to enter next epoch
                        WaitForNextEpoch();
                    }

                    var isBlocked = waitingJobs.Count > 0 && waitingJobs.Peek().NotificationId == id;
                    if (!flags.HasFlag(JobFlags.SynchronizedBlocking) && !isBlocked)
                    {
                        // Execute waiting jobs first
                        while (waitingJobs.Count > 0)
                        {
                            var (waitingJob, _) = waitingJobs.Dequeue();
                            waitingJob.RunSynchronously();
                        }

                        // Execute this job
                        job.RunSynchronously();
                    }
                    else
                    {
                        // This job can not be executed right now
                        // We need to ensure there is a continuation available
                        waitingJobs.Enqueue((job, id));
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                // Do nothing - this is a way to terminate blocked threads
            }
        }

        public void Start()
            => workerThread.Start();

        public void Execute(ulong notificationId, JobFlags flags, Task job)
            => jobs.Add((job, notificationId, flags));

        public void SetName(string name)
            => DisplayName = name;

        public StackFrame PeekCallstack()
            => callstack.Peek();

        public int GetCallstackDepth()
            => callstack.Count;

        public void PushCallStack(FunctionInfo info, MethodInterpretation interpretation, object? args = null)
            => callstack.Push(new(info, interpretation, args));

        public void EnterState(ShadowThreadState newState)
            => State = newState;

        public StackFrame PopCallStack()
            => callstack.Pop();

        public void EnterNewEpoch(ulong? newValue = null)
            => Epoch = (newValue.HasValue) ? newValue.Value : Epoch + 1;

        private void WaitForNextEpoch()
        {
            if (schedulerEpoch < Epoch)
            {
                lock (schedulerEpochChangeSignaler)
                {
                    while (schedulerEpoch < Epoch)
                        Monitor.Wait(schedulerEpochChangeSignaler);
                }
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                jobs.CompleteAdding();
                if (workerThread.ThreadState != ThreadState.Unstarted)
                {
                    // Wait for some time
                    // Then issue interrupts (thread could be blocked)
                    while (!workerThread.Join(timeout: TimeSpan.FromSeconds(3)))
                        workerThread.Interrupt();
                }
                // Unregister event handler
                schedulerEpochChangeSignaler.EpochChanged -= OnSchedulerEpochChanged;
                GC.SuppressFinalize(this);
            }
        }
    }
}