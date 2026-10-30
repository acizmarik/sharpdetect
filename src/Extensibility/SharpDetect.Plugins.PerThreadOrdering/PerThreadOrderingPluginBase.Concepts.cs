// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    // Acquire/release: mutual exclusion with an owner
    public event Action<LockAcquireAttemptArgs>? LockAcquireAttempted;
    public event Action<LockAcquireResultArgs>? LockAcquireReturned;
    public event Action<LockReleaseArgs>? LockReleased;

    // Condition variable bound to a lock
    public event Action<ObjectPulseOneArgs>? ObjectPulsedOne;
    public event Action<ObjectPulseAllArgs>? ObjectPulsedAll;
    public event Action<ObjectWaitAttemptArgs>? ObjectWaitAttempted;
    public event Action<ObjectWaitResultArgs>? ObjectWaitReturned;

    // Signal/wait: no owner, waiters observe every signal
    public event Action<SignalCreatedArgs>? SignalCreated;
    public event Action<SignalSetArgs>? Signaled;
    public event Action<SignalResetArgs>? SignalReset;
    public event Action<SignalWaitResultArgs>? SignalWaitReturned;

    // Permits: counted handover from a releaser to one acquirer
    public event Action<PermitsCreatedArgs>? PermitsCreated;
    public event Action<PermitAcquireAttemptArgs>? PermitAcquireAttempted;
    public event Action<PermitAcquireResultArgs>? PermitAcquireReturned;
    public event Action<PermitsReleaseArgs>? PermitsReleased;
    public event Action<PermitsReleaseArgs>? PermitsReleaseRetracted;

    // Fork/start/finish/join: a child unit of work keyed by an object
    public event Action<ForkRequestArgs>? ForkRequested;
    public event Action<ForkStartArgs>? ForkStarted;
    public event Action<ForkFinishArgs>? ForkFinished;
    public event Action<ForkJoinArgs>? ForkJoined;

    // Thread joins are keyed by the thread, not by an object
    public event Action<ThreadJoinAttemptArgs>? ThreadJoinAttempted;
    public event Action<ThreadJoinResultArgs>? ThreadJoinReturned;

    // Publish/observe: a value handed over through a container
    public event Action<ValuePublicationArgs>? ValuePublication;

    private void RaiseLockAcquireAttempted(in LockAcquireAttemptArgs args) => LockAcquireAttempted?.Invoke(args);
    private void RaiseLockAcquireReturned(in LockAcquireResultArgs args) => LockAcquireReturned?.Invoke(args);
    private void RaiseLockReleased(in LockReleaseArgs args) => LockReleased?.Invoke(args);
    private void RaiseObjectPulsedOne(in ObjectPulseOneArgs args) => ObjectPulsedOne?.Invoke(args);
    private void RaiseObjectPulsedAll(in ObjectPulseAllArgs args) => ObjectPulsedAll?.Invoke(args);
    private void RaiseObjectWaitAttempted(in ObjectWaitAttemptArgs args) => ObjectWaitAttempted?.Invoke(args);
    private void RaiseObjectWaitReturned(in ObjectWaitResultArgs args) => ObjectWaitReturned?.Invoke(args);
    private void RaiseSignalCreated(in SignalCreatedArgs args) => SignalCreated?.Invoke(args);
    private void RaiseSignaled(in SignalSetArgs args) => Signaled?.Invoke(args);
    private void RaiseSignalReset(in SignalResetArgs args) => SignalReset?.Invoke(args);
    private void RaiseSignalWaitReturned(in SignalWaitResultArgs args) => SignalWaitReturned?.Invoke(args);
    private void RaisePermitsCreated(in PermitsCreatedArgs args) => PermitsCreated?.Invoke(args);
    private void RaisePermitAcquireAttempted(in PermitAcquireAttemptArgs args) => PermitAcquireAttempted?.Invoke(args);
    private void RaisePermitAcquireReturned(in PermitAcquireResultArgs args) => PermitAcquireReturned?.Invoke(args);
    private void RaisePermitsReleased(in PermitsReleaseArgs args) => PermitsReleased?.Invoke(args);
    private void RaisePermitsReleaseRetracted(in PermitsReleaseArgs args) => PermitsReleaseRetracted?.Invoke(args);
    private void RaiseThreadJoinAttempted(in ThreadJoinAttemptArgs args) => ThreadJoinAttempted?.Invoke(args);
    private void RaiseThreadJoinReturned(in ThreadJoinResultArgs args) => ThreadJoinReturned?.Invoke(args);

    private void RaiseForkRequested(ProcessThreadId id, ProcessTrackedObjectId childId, ForkKind kind)
    {
        if (childId.ObjectId.Value == 0)
            return;

        ForkRequested?.Invoke(new ForkRequestArgs(id, childId, kind));
    }

    private void RaiseForkStarted(ProcessThreadId id, ProcessTrackedObjectId childId, ForkKind kind)
    {
        if (childId.ObjectId.Value == 0)
            return;

        ForkStarted?.Invoke(new ForkStartArgs(id, childId, kind));
    }

    private void RaiseForkFinished(ProcessThreadId id, ProcessTrackedObjectId childId, ForkKind kind, ForkFinishKind finishKind)
    {
        if (childId.ObjectId.Value == 0)
            return;

        ForkFinished?.Invoke(new ForkFinishArgs(id, childId, kind, finishKind));
    }

    private void RaiseForkJoined(ProcessThreadId id, ProcessTrackedObjectId childId, TaskJoinOutcome outcome)
        => ForkJoined?.Invoke(new ForkJoinArgs(id, childId, outcome));

    private void RaiseValuePublication(
        ProcessThreadId id,
        ProcessTrackedObjectId container,
        ProcessTrackedObjectId value,
        ValuePublicationKind kind)
    {
        if (value.ObjectId.Value == 0 || container.ObjectId.Value == 0)
            return;

        ValuePublication?.Invoke(new ValuePublicationArgs(id, container, value, kind));
    }
}
