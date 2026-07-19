// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using Xunit;

namespace SharpDetect.Core.Tests.Events;

public class ArgumentValueTests
{
    [Fact]
    public void Primitive_RoundTrips()
    {
        var value = ArgumentValue.Primitive(42, PrimitiveKind.I4);

        Assert.Equal(42, (int)value.AsPrimitive);
    }

    [Fact]
    public void Tracked_RoundTrips()
    {
        var value = ArgumentValue.Tracked(new TrackedObjectId(0x1234));

        Assert.Equal(new TrackedObjectId(0x1234), value.AsTrackedObject);
    }

    [Fact]
    public void TrackedArray_RoundTrips()
    {
        var array = new[] { new TrackedObjectId(1), new TrackedObjectId(2) };
        var value = ArgumentValue.TrackedArray(array);

        Assert.Equal(array, value.AsTrackedObjectArray);
    }

    [Fact]
    public void AsPrimitive_OnTrackedObject_Throws()
    {
        var value = ArgumentValue.Tracked(new TrackedObjectId(1));

        Assert.Throws<InvalidOperationException>(() => value.AsPrimitive);
    }

    [Fact]
    public void AsTrackedObject_OnPrimitive_Throws()
    {
        var value = ArgumentValue.Primitive(1, PrimitiveKind.I4);

        Assert.Throws<InvalidOperationException>(() => value.AsTrackedObject);
    }

    [Fact]
    public void AsTrackedObject_OnTrackedArray_Throws()
    {
        var value = ArgumentValue.TrackedArray([new TrackedObjectId(1)]);

        Assert.Throws<InvalidOperationException>(() => value.AsTrackedObject);
    }

    [Fact]
    public void AsTrackedObjectArray_OnTrackedObject_Throws()
    {
        var value = ArgumentValue.Tracked(new TrackedObjectId(1));

        Assert.Throws<InvalidOperationException>(() => value.AsTrackedObjectArray);
    }

    [Fact]
    public void AsTrackedObject_NullPointer_IsPreserved()
    {
        var value = ArgumentValue.Tracked(new TrackedObjectId(0));

        Assert.Equal(new TrackedObjectId(0), value.AsTrackedObject);
    }
}
