// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Synchronization;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class HeaderValidationTests : InterProcessQueueTestsBase
{
    public HeaderValidationTests()
        : base(
            queueName: "SharpDetect_IPQ_HeaderValidation_Test_Queue",
            queueFile: "SharpDetect_IPQ_HeaderValidation_Test.data",
            size: 1024 * 1024,
            semaphoreName: "SHARPDETECT_IPQ_HeaderValidation_Test_Semaphore")
    {

    }

    [Fact]
    public void InterProcessQueue_Attach_CapacityMismatch_Throws()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        using var producer = new Producer(producerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));

        // Act
        var mismatchedConsumerOptions = new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize + 4096, TestSemaphoreName);

        // Assert
        Assert.Throws<QueueHeaderValidationException>(() => new Consumer(mismatchedConsumerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: false)));
    }

    [Fact]
    public void InterProcessQueue_Attach_MatchingCapacity_Succeeds()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        using var producer = new Producer(producerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);

        // Act
        using var consumer = new Consumer(consumerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: false));

        // Assert
        var result = consumer.TryDequeue();
        Assert.Equal(DequeueErrorType.NothingToRead, result.Error);
    }
}
