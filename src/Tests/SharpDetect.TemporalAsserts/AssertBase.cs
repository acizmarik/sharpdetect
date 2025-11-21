// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TemporalAsserts;

public abstract class AssertBase<TId, TType>
    where TId : IComparable<TId>
{
    public AssertStatus Status { get; protected set; }

    public abstract void ProcessEvent(IEvent<TId, TType> @event);
    public abstract void Complete();
    public abstract string GetDiagnosticInfo();

    public AssertStatus Evaluate(IEnumerable<IEvent<TId, TType>> events)
    {
        foreach (var @event in events)
            ProcessEvent(@event);

        Complete();
        return Status;
    }
}