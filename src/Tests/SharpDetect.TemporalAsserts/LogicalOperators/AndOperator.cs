// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TemporalAsserts.LogicalOperators;

public sealed class AndOperator<TId, TType> : AssertBase<TId, TType>
    where TId : IComparable<TId>
{
    private readonly AssertBase<TId, TType> _left;
    private readonly AssertBase<TId, TType> _right;

    public AndOperator(AssertBase<TId, TType> left, AssertBase<TId, TType> right)
    {
        _left = left;
        _right = right;
        Status = AssertStatus.Unknown;
    }
    
    public override void ProcessEvent(IEvent<TId, TType> @event)
    {
        if (_left.Status == AssertStatus.Unknown)
            _left.ProcessEvent(@event);

        if (_left.Status == AssertStatus.Unknown)
            return;

        if (_left.Status == AssertStatus.Violated)
        {
            Status = AssertStatus.Violated;
            return;
        }

        if (_right.Status == AssertStatus.Unknown)
            _right.ProcessEvent(@event);

        if (_right.Status == AssertStatus.Unknown)
            return;

        if (_right.Status == AssertStatus.Violated)
        {
            Status = AssertStatus.Violated;
            return;
        }
        
        Status = AssertStatus.Satisfied;
    }

    public override void Complete()
    {
        _left.Complete();
        _right.Complete();
        Status = _left.Status == AssertStatus.Satisfied && _right.Status == AssertStatus.Satisfied
            ? AssertStatus.Satisfied
            : AssertStatus.Violated;
    }

    public override string GetDiagnosticInfo()
    {
        return $"({_left.GetDiagnosticInfo()}) And ({_right.GetDiagnosticInfo()}) [Status: {Status}]";
    }
}