// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TemporalAsserts.LogicalOperators;

public sealed class NotOperator<TId, TType> : AssertBase<TId, TType>
    where TId : IComparable<TId>
{
    private readonly AssertBase<TId, TType> _argument;

    public NotOperator(AssertBase<TId, TType> argument)
    {
        _argument = argument;
        Status = AssertStatus.Unknown;
    }
    
    public override void ProcessEvent(IEvent<TId, TType> @event)
    {
        _argument.ProcessEvent(@event);
        Evaluate();
    }

    public override void Complete()
    {
        _argument.Complete();
        Evaluate();
    }

    private void Evaluate()
    {
        Status = _argument.Status switch
        {
            AssertStatus.Satisfied => AssertStatus.Violated,
            AssertStatus.Violated => AssertStatus.Satisfied,
            _ => AssertStatus.Unknown
        };
    }

    public override string GetDiagnosticInfo()
    {
        return $"Not({_argument.GetDiagnosticInfo()}) [Status: {Status}]";
    }
}
