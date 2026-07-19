// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using Xunit;

namespace SharpDetect.Core.Tests.Events;

public class PrimitiveArgumentTests
{
    [Fact]
    public void Bool_RoundTrips()
    {
        Assert.True((bool)new PrimitiveArgument(1, PrimitiveKind.Boolean));
        Assert.False((bool)new PrimitiveArgument(0, PrimitiveKind.Boolean));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void Int_RoundTrips(int expected)
    {
        var argument = new PrimitiveArgument(unchecked((ulong)(long)expected), PrimitiveKind.I4);

        Assert.Equal(expected, (int)argument);
    }

    [Fact]
    public void Long_RoundTrips()
    {
        var argument = new PrimitiveArgument(unchecked((ulong)long.MinValue), PrimitiveKind.I8);

        Assert.Equal(long.MinValue, (long)argument);
    }

    [Fact]
    public void Float_RoundTrips()
    {
        var argument = new PrimitiveArgument(BitConverter.SingleToUInt32Bits(1.5f), PrimitiveKind.R4);

        Assert.Equal(1.5f, (float)argument);
    }

    [Fact]
    public void Double_RoundTrips()
    {
        var argument = new PrimitiveArgument(BitConverter.DoubleToUInt64Bits(1.5d), PrimitiveKind.R8);

        Assert.Equal(1.5d, (double)argument);
    }

    [Fact]
    public void Char_RoundTrips()
    {
        Assert.Equal('x', (char)new PrimitiveArgument('x', PrimitiveKind.Char));
    }

    [Fact]
    public void Int_OnFloat_Throws()
    {
        var argument = new PrimitiveArgument(BitConverter.SingleToUInt32Bits(1.0f), PrimitiveKind.R4);

        Assert.Throws<InvalidOperationException>(() => (int)argument);
    }

    [Fact]
    public void Int_OnDouble_Throws()
    {
        var argument = new PrimitiveArgument(BitConverter.DoubleToUInt64Bits(1.0d), PrimitiveKind.R8);

        Assert.Throws<InvalidOperationException>(() => (int)argument);
    }

    [Fact]
    public void Float_OnInt_Throws()
    {
        var argument = new PrimitiveArgument(1, PrimitiveKind.I4);

        Assert.Throws<InvalidOperationException>(() => (float)argument);
    }

    [Fact]
    public void Double_OnFloat_Throws()
    {
        var argument = new PrimitiveArgument(BitConverter.SingleToUInt32Bits(1.0f), PrimitiveKind.R4);

        Assert.Throws<InvalidOperationException>(() => (double)argument);
    }

    [Fact]
    public void Bool_OnInt_Throws()
    {
        var argument = new PrimitiveArgument(1, PrimitiveKind.I4);

        Assert.Throws<InvalidOperationException>(() => (bool)argument);
    }

    [Fact]
    public void IntegralConversions_DoNotCheckWidth()
    {
        var argument = new PrimitiveArgument(unchecked((ulong)(long)0x1_0000_0001L), PrimitiveKind.I8);

        Assert.Equal(1, (int)argument);
    }

    [Fact]
    public void Kind_IsExposed()
    {
        Assert.Equal(PrimitiveKind.U2, new PrimitiveArgument(1, PrimitiveKind.U2).Kind);
    }
}
