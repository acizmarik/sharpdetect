// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class RegistrationTableTests : IDisposable
{
    private readonly string TableName = $"SharpDetect_IPQ_Registration_Test_Table_{Guid.NewGuid():N}";
    private readonly string TableFile = $"SharpDetect_IPQ_Registration_Test_{Guid.NewGuid():N}.data";
    private const long TableCapacity = 65_536;
    private bool _disposed;

    private RegistrationTable CreateConsumer()
        => new(TableName, TableFile, TableCapacity, createAsConsumer: true);

    private RegistrationTable CreateProducer()
        => new(TableName, TableFile, TableCapacity, createAsConsumer: false);

    [Fact]
    public void RegistrationTable_Register_ThenDrain_ReturnsPidsInOrder()
    {
        using var consumer = CreateConsumer();
        using var producer = CreateProducer();

        producer.Register(11);
        producer.Register(22);
        producer.Register(33);

        Assert.Equal([11, 22, 33], consumer.DrainNewRegistrations());
    }

    [Fact]
    public void RegistrationTable_Drain_ReturnsOnlyNewPidsSinceLastCall()
    {
        using var consumer = CreateConsumer();
        using var producer = CreateProducer();

        producer.Register(11);
        Assert.Equal([11], consumer.DrainNewRegistrations());

        // Nothing new yet.
        Assert.Empty(consumer.DrainNewRegistrations());

        producer.Register(22);
        Assert.Equal([22], consumer.DrainNewRegistrations());
    }

    [Fact]
    public void RegistrationTable_Register_IsIdempotent()
    {
        using var consumer = CreateConsumer();
        using var producer = CreateProducer();

        producer.Register(42);
        producer.Register(42);
        producer.Register(42);

        Assert.Equal([42], consumer.DrainNewRegistrations());
    }

    [Fact]
    public void RegistrationTable_ConcurrentRegistrations_AllVisibleExactlyOnce()
    {
        using var consumer = CreateConsumer();
        
        const int count = 200;
        using var producer = CreateProducer();
        Parallel.For(1, count + 1, i => producer.Register((uint)i));

        var seen = new HashSet<uint>();
        for (var attempt = 0; attempt < 100 && seen.Count < count; attempt++)
        {
            foreach (var pid in consumer.DrainNewRegistrations())
                Assert.True(seen.Add(pid), $"PID {pid} was returned more than once.");
        }

        Assert.Equal(count, seen.Count);
        Assert.Equal(Enumerable.Range(1, count).Select(i => (uint)i).OrderBy(x => x), seen.OrderBy(x => x));
    }

    [Fact]
    public void RegistrationTable_Attach_CapacityMismatch_Throws()
    {
        using var consumer = CreateConsumer();
        Assert.Throws<QueueHeaderValidationException>(() =>
            new RegistrationTable(TableName, TableFile, TableCapacity + 4096, createAsConsumer: false));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (File.Exists(TableFile))
            File.Delete(TableFile);
    }
}
