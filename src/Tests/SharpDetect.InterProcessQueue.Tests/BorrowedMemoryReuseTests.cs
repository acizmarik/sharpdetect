// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Synchronization;
using System.Buffers;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class BorrowedMemoryReuseTests : InterProcessQueueTestsBase
{
    public BorrowedMemoryReuseTests()
        : base(
            queueName: "SharpDetect_IPQ_BorrowedMemoryReuse_Test_Queue",
            queueFile: "SharpDetect_IPQ_BorrowedMemoryReuse_Test.data",
            size: 1024 * 1024,
            semaphoreName: "SHARPDETECT_IPQ_BorrowedMemoryReuse_Test_Semaphore")
    {

    }

    private Producer CreateProducer()
        => new(
            new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName),
            InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));

    private Consumer CreatePooledConsumer()
        => new(
            new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName),
            InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: false),
            ArrayPool<byte>.Shared);

    [Fact]
    public void Dequeue_DisposingEachResult_ReturnsEachMessage()
    {
        using var producer = CreateProducer();
        using var consumer = CreatePooledConsumer();
        producer.TryEnqueue("first"u8.ToArray());
        producer.TryEnqueue("second"u8.ToArray());

        var first = consumer.TryDequeue();
        Assert.True(first.IsSuccess);
        Assert.Equal("first"u8.ToArray(), first.Value.Get().ToArray());
        (first.Value as IDisposable)?.Dispose();

        var second = consumer.TryDequeue();
        Assert.True(second.IsSuccess);
        Assert.Equal("second"u8.ToArray(), second.Value.Get().ToArray());
        (second.Value as IDisposable)?.Dispose();
    }

    [Fact]
    public void Dequeue_WithPreviousResultStillAlive_Throws()
    {
        using var producer = CreateProducer();
        using var consumer = CreatePooledConsumer();
        producer.TryEnqueue("first"u8.ToArray());
        producer.TryEnqueue("second"u8.ToArray());

        var first = consumer.TryDequeue();
        Assert.True(first.IsSuccess);

        Assert.Throws<InvalidOperationException>(() => consumer.TryDequeue());
    }

    [Fact]
    public void Get_AfterDispose_Throws()
    {
        using var producer = CreateProducer();
        using var consumer = CreatePooledConsumer();
        producer.TryEnqueue("first"u8.ToArray());

        var result = consumer.TryDequeue();
        (result.Value as IDisposable)?.Dispose();

        Assert.Throws<ObjectDisposedException>(() => result.Value.Get());
    }
}
