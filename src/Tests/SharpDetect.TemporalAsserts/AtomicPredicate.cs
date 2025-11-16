// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TemporalAsserts;

public class AtomicPredicate<TId, TType> : AssertBase<TId, TType>
    where TId : IComparable<TId>
{
    private readonly Predicate<IEvent<TId, TType>> _predicate;

    public AtomicPredicate(Predicate<IEvent<TId, TType>> predicate)
    {
        _predicate = predicate;
        Status = AssertStatus.Unknown;
    }
    
    public override void ProcessEvent(IEvent<TId, TType> @event)
    {
        Status = _predicate(@event) ? AssertStatus.Satisfied : AssertStatus.Violated;
    }

    public override void Complete()
    {
        /* Nothing to do */
    }
}