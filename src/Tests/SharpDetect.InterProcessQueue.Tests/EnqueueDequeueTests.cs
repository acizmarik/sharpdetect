// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Synchronization;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class EnqueueDequeueTests : InterProcessQueueTestsBase
{
    public EnqueueDequeueTests()
        : base(
            queueName: "SharpDetect_IPQ_EnqueueDequeue_Test_Queue",
            queueFile: "SharpDetect_IPQ_EnqueueDequeue__Test.data",
            size: 1024 * 1024,
            semaphoreName: "SHARPDETECT_IPQ_EnqueueDequeue_Test_Semaphore")
    {
        
    }
    
    [Fact]
    public void InterProcessQueue_Enqueue_SingleMessage_Succeeds()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        using var producer = new Producer(producerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));
        using var consumer = new Consumer(consumerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: false));
        
        var message = "Hello, World!"u8.ToArray();
        
        // Act
        var enqueueResult = producer.TryEnqueue(message);
        
        // Assert
        Assert.True(enqueueResult.IsSuccess);
        Assert.False(enqueueResult.IsError);
    }

    [Fact]
    public void InterProcessQueue_Dequeue_SingleMessage_ReturnsCorrectData()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        using var producer = new Producer(producerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));
        using var consumer = new Consumer(consumerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: false));
        
        var expectedMessage = "Test Message"u8.ToArray();
        producer.TryEnqueue(expectedMessage);
        
        // Act
        var dequeueResult = consumer.TryDequeue();
        
        // Assert
        Assert.True(dequeueResult.IsSuccess);
        var memory = dequeueResult.Value;
        var actualMessage = memory.GetLocalMemory().ToArray();
        (memory as IDisposable)?.Dispose();
        
        Assert.Equal(expectedMessage, actualMessage);
    }

    [Fact]
    public void InterProcessQueue_EnqueueDequeue_MultipleMessages_MaintainsOrder()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        using var producer = new Producer(producerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));
        using var consumer = new Consumer(consumerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: false));
        
        var messages = new[]
        {
            "First"u8.ToArray(),
            "Second"u8.ToArray(),
            "Third"u8.ToArray()
        };
        
        // Act
        foreach (var message in messages)
            producer.TryEnqueue(message);
        
        // Assert
        foreach (var message in messages)
        {
            var dequeueResult = consumer.TryDequeue();
            Assert.True(dequeueResult.IsSuccess);
            
            var memory = dequeueResult.Value;
            var actualMessage = memory.GetLocalMemory().ToArray();
            (memory as IDisposable)?.Dispose();
            
            Assert.Equal(message, actualMessage);
        }
    }

    [Fact]
    public void InterProcessQueue_Enqueue_MessageLargerThanQueue_ReturnsNotEnoughMemory()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        using var producer = new Producer(producerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));
        
        // Act
        var result = producer.TryEnqueue(new byte[TestQueueSize]);
        
        // Assert
        Assert.True(result.IsError);
        Assert.Equal(EnqueueErrorType.NotEnoughFreeMemory, result.Error);
    }
}
