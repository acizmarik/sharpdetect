// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.Synchronization;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class LockTokenTests
{
    [Fact]
    public void LockToken_Next_ReturnsNonZero()
    {
        var token = LockToken.Next();
        Assert.NotEqual(0L, token);
    }

    [Fact]
    public void LockToken_Next_TwoConsecutiveCalls_ReturnDifferentValues()
    {
        var token1 = LockToken.Next();
        var token2 = LockToken.Next();
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void LockToken_Next_EmbedsPidInUpperBits()
    {
        var expectedPid = (uint)Environment.ProcessId;
        for (var i = 0; i < 100; i++)
        {
            var token = LockToken.Next();
            var embeddedPid = (uint)((ulong)token >> 32);
            Assert.Equal(expectedPid, embeddedPid);
        }
    }
}
