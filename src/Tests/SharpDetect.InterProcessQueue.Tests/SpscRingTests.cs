// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Synchronization;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class SpscRingTests : InterProcessQueueTestsBase
{
    public SpscRingTests()
        : base(
            queueName: "SharpDetect_IPQ_SpscRing_Test_Queue",
            queueFile: "SharpDetect_IPQ_SpscRing_Test.data",
            size: 512,
            semaphoreName: "SHARPDETECT_IPQ_SpscRing_Test_Semaphore")
    {

    }

    [Fact]
    public void SpscRing_InterleavedEnqueueDequeue_AcrossWrapBoundary_PreservesData()
    {
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        using var producer = new Producer(producerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));
        using var consumer = new Consumer(consumerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: false));
        
        var random = new Random(1234);
        for (var i = 0; i < 500; i++)
        {
            var payload = new byte[random.Next(1, 64)];
            random.NextBytes(payload);

            var enqueue = producer.TryEnqueue(payload);
            Assert.True(enqueue.IsSuccess);

            var dequeue = consumer.TryDequeue();
            Assert.True(dequeue.IsSuccess);
            var actual = dequeue.Value.GetLocalMemory().ToArray();
            (dequeue.Value as IDisposable)?.Dispose();

            Assert.Equal(payload, actual);
        }
    }

    [Fact]
    public void SpscRing_FillUntilFull_ThenDrain_ThenRefills()
    {
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        using var producer = new Producer(producerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));
        using var consumer = new Consumer(consumerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: false));

        var payload = new byte[32];
        for (var i = 0; i < payload.Length; i++)
            payload[i] = (byte)i;

        // Fill until the producer reports the ring is full.
        var accepted = 0;
        while (producer.TryEnqueue(payload).IsSuccess)
            accepted++;
        Assert.True(accepted > 0);

        // Drain everything back out, in order.
        for (var i = 0; i < accepted; i++)
        {
            var dequeue = consumer.TryDequeue();
            Assert.True(dequeue.IsSuccess);
            var actual = dequeue.Value.GetLocalMemory().ToArray();
            (dequeue.Value as IDisposable)?.Dispose();
            Assert.Equal(payload, actual);
        }

        // Ring is empty again.
        Assert.Equal(DequeueErrorType.NothingToRead, consumer.TryDequeue().Error);
        Assert.True(producer.TryEnqueue(payload).IsSuccess);
        Assert.True(consumer.TryDequeue().IsSuccess);
    }
}
