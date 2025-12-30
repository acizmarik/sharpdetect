// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.TemporalAsserts;

public class Event<TId, TType, TContent>(TId id, TType type, TContent content) : IEvent<TId, TType>
    where TId : IComparable<TId>
    where TContent : notnull
{
    public TId Id { get; } = id;
    public TType Type { get; } = type;
    private readonly Dictionary<Type, object> _data = new() { { typeof(TContent), content } };

    public TContent Content => content;
    
    public T Get<T>()
        where T : notnull
    {
        if (!_data.TryGetValue(typeof(T), out var value))
            throw new InvalidOperationException($"Event does not contain data of type {typeof(T).FullName}.");
        if (value is not T typedValue)
            throw new InvalidOperationException($"Event data of type {typeof(T).FullName} is not of expected type.");

        return typedValue;
    }

    public void Set<T>(T data)
        where T : notnull
    {
        _data[typeof(T)] = data;
    }
}