// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Events;

/// <summary>
/// Discriminated union for a parsed argument value
/// </summary>
public readonly struct ArgumentValue
{
    private enum ValueKind : byte
    {
        Primitive,
        TrackedObject,
        TrackedObjectArray,
    }

    private readonly ulong _scalar;
    private readonly ValueKind _kind;
    private readonly PrimitiveKind _primitiveKind;

    private ArgumentValue(ulong scalar, TrackedObjectId[]? array, ValueKind kind, PrimitiveKind primitiveKind = default)
    {
        _scalar = scalar;
        AsTrackedObjectArray = array;
        _kind = kind;
        _primitiveKind = primitiveKind;
    }

    public static ArgumentValue Primitive(ulong bits, PrimitiveKind primitiveKind)
        => new(bits, null, ValueKind.Primitive, primitiveKind);

    public static ArgumentValue Tracked(TrackedObjectId id)
        => new(id.Value, null, ValueKind.TrackedObject);

    public static ArgumentValue TrackedArray(TrackedObjectId[] array)
        => new(0, array, ValueKind.TrackedObjectArray);

    public PrimitiveArgument AsPrimitive => _kind == ValueKind.Primitive
        ? new PrimitiveArgument(_scalar, _primitiveKind)
        : throw new InvalidOperationException($"Argument value is {_kind}, not a primitive.");

    public TrackedObjectId AsTrackedObject => _kind == ValueKind.TrackedObject
        ? new TrackedObjectId((nuint)_scalar)
        : throw new InvalidOperationException($"Argument value is {_kind}, not a tracked object.");

    public TrackedObjectId[] AsTrackedObjectArray => _kind == ValueKind.TrackedObjectArray
        ? field!
        : throw new InvalidOperationException($"Argument value is {_kind}, not a tracked object array.");
}
