// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.Synchronization;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class InterProcessSemaphoreTests : IDisposable
{
    private const string SemaphoreName = "/SharpDetect_IPQ_Semaphore_Tests";
    private bool _disposed;

    [Fact]
    public void InterProcessSemaphore_CreateOrOpen_AsOwner_Succeeds()
    {
        // Act & Assert — no exception
        using var semaphore = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);
        Assert.NotNull(semaphore);
    }

    [Fact]
    public void InterProcessSemaphore_CreateOrOpen_AsNonOwner_Succeeds()
    {
        // A non-owner opens (or creates) the same named semaphore
        using var owner = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);
        using var nonOwner = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: false);
        Assert.NotNull(nonOwner);
    }

    [Fact]
    public void InterProcessSemaphore_Wait_AfterRelease_ReturnsTrue()
    {
        // Arrange
        using var semaphore = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);

        // Act
        semaphore.Release();
        var acquired = semaphore.Wait(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(acquired);
    }

    [Fact]
    public void InterProcessSemaphore_Wait_WithoutRelease_TimesOut()
    {
        // Arrange
        using var semaphore = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);

        // Act
        var acquired = semaphore.Wait(TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.False(acquired);
    }

    [Fact]
    public void InterProcessSemaphore_Release_MultipleTimesAllowsMultipleWaits()
    {
        // Arrange
        using var semaphore = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);
        const int count = 3;

        // Act
        for (var i = 0; i < count; i++)
            semaphore.Release();

        // Assert
        for (var i = 0; i < count; i++)
            Assert.True(semaphore.Wait(TimeSpan.FromSeconds(5)));

        // And the next wait should time out
        Assert.False(semaphore.Wait(TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public void InterProcessSemaphore_CrossInstance_OwnerReleases_NonOwnerAcquires()
    {
        // Arrange
        using var owner = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);
        using var nonOwner = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: false);

        // Act
        owner.Release();
        var acquired = nonOwner.Wait(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(acquired);
    }

    [Fact]
    public void InterProcessSemaphore_CrossInstance_NonOwnerReleases_OwnerAcquires()
    {
        // Arrange
        using var owner = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);
        using var nonOwner = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: false);

        // Act
        nonOwner.Release();
        var acquired = owner.Wait(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(acquired);
    }

    [Fact]
    public void InterProcessSemaphore_Dispose_DoesNotThrow()
    {
        // Arrange
        var semaphore = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);

        // Act & Assert — no exception
        semaphore.Dispose();
    }

    [Fact]
    public void InterProcessSemaphore_Dispose_NonOwner_DoesNotThrow()
    {
        // Arrange
        using var owner = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);
        var nonOwner = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: false);

        // Non-owner disposes first — should not unlink, so owner can still dispose cleanly
        nonOwner.Dispose();
        owner.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Best-effort cleanup: open as owner to unlink any leftover named semaphore
        try
        {
            using var cleanup = InterProcessSemaphore.CreateOrOpen(SemaphoreName, isOwner: true);
        }
        catch
        {
            // Ignore — semaphore may already be unlinked by the test
        }
    }
}
