// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TemporalAsserts.TemporalOperators;

public sealed class EventuallyOperator<TId, TType> : AssertBase<TId, TType>
    where TId : IComparable<TId>
{
    private readonly AssertBase<TId, TType> _argument;
    
    public EventuallyOperator(AssertBase<TId, TType> argument)
    {
        _argument = argument;
        Status = AssertStatus.Unknown;
    }
    
    public override void ProcessEvent(IEvent<TId, TType> @event)
    {
        if (Status == AssertStatus.Satisfied)
            return;
            
        _argument.ProcessEvent(@event);
        if (_argument.Status == AssertStatus.Satisfied)
            Status = AssertStatus.Satisfied;
    }

    public override void Complete()
    {
        _argument.Complete();
        if (Status == AssertStatus.Unknown)
            Status = AssertStatus.Violated;
    }
}