// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TemporalAsserts;

public interface IEvent<out TId, out TType>
    where TId : IComparable<TId>
{
    TId Id { get; }
    TType Type { get; }
    
    T Get<T>() where T : notnull;
    void Set<T>(T data) where T : notnull;
}