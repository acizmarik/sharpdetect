// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.TemporalAsserts.LogicalOperators;

namespace SharpDetect.TemporalAsserts.TemporalOperators;

public static class TemporalOperatorExtensions
{
    public static AssertBase<TId, TType> Then<TId, TType>(
        this AssertBase<TId, TType> first,
        AssertBase<TId, TType> second)
        where TId : IComparable<TId>
    {
        return new AndOperator<TId, TType>(
            first, 
            second);
    }
}