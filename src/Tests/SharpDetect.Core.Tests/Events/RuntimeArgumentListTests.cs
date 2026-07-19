// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using System.Buffers;
using Xunit;

namespace SharpDetect.Core.Tests.Events;

public class RuntimeArgumentListTests
{
    private static RuntimeArgumentList Rent(params ArgumentValue[] values)
    {
        var arguments = ArrayPool<RuntimeArgumentInfo>.Shared.Rent(values.Length);
        for (var i = 0; i < values.Length; i++)
            arguments[i] = new RuntimeArgumentInfo((ushort)i, values[i]);

        return RuntimeArgumentList.Rent(arguments, values.Length);
    }

    private static ArgumentValue Int(int value)
        => ArgumentValue.Primitive(unchecked((ulong)(long)value), PrimitiveKind.I4);

    [Fact]
    public void Count_IsTheArgumentCount_NotTheBufferLength()
    {
        using var list = Rent(Int(1), Int(2));

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void Indexer_ReturnsArgumentsInOrder()
    {
        using var list = Rent(Int(10), Int(20));

        Assert.Equal(10, (int)list[0].Value.AsPrimitive);
        Assert.Equal(20, (int)list[1].Value.AsPrimitive);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Indexer_OutOfRange_Throws(int index)
    {
        using var list = Rent(Int(1), Int(2));

        Assert.Throws<IndexOutOfRangeException>(() => list[index]);
    }

    [Fact]
    public void Enumeration_StopsAtCount()
    {
        using var list = Rent(Int(1), Int(2));

        Assert.Equal(2, list.Count());
    }

    [Fact]
    public void Empty_HasNoArguments()
    {
        using var list = Rent();

        Assert.Empty(list);
    }

    [Fact]
    public void Rent_AfterDispose_ProducesUsableList()
    {
        Rent(Int(1)).Dispose();

        using var list = Rent(Int(2), Int(3));

        Assert.Equal(2, list.Count);
        Assert.Equal(2, (int)list[0].Value.AsPrimitive);
        Assert.Equal(3, (int)list[1].Value.AsPrimitive);
    }
}
