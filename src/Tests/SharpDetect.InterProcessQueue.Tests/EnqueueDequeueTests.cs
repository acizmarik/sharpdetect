// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.Configuration;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class EnqueueDequeueTests
{
    [Fact]
    public void InterProcessQueue_Enqueue_SingleMessage_Succeeds()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(
            InitializationTests.TestQueueName,
            InitializationTests.TestFileName,
            InitializationTests.TestQueueSize);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(
            InitializationTests.TestQueueName,
            InitializationTests.TestFileName,
            InitializationTests.TestQueueSize);
        using var producer = new Producer(producerOptions);
        using var consumer = new Consumer(consumerOptions);
        
        var message = "Hello, World!"u8.ToArray();
        
        // Act
        var enqueueResult = producer.Enqueue(message);
        
        // Assert
        Assert.True(enqueueResult.IsSuccess);
        Assert.False(enqueueResult.IsError);
    }

    [Fact]
    public void InterProcessQueue_Dequeue_SingleMessage_ReturnsCorrectData()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(
            InitializationTests.TestQueueName,
            InitializationTests.TestFileName,
            InitializationTests.TestQueueSize);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(
            InitializationTests.TestQueueName,
            InitializationTests.TestFileName,
            InitializationTests.TestQueueSize);
        using var producer = new Producer(producerOptions);
        using var consumer = new Consumer(consumerOptions);
        
        var expectedMessage = "Test Message"u8.ToArray();
        producer.Enqueue(expectedMessage);
        
        // Act
        var dequeueResult = consumer.Dequeue();
        
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
        var producerOptions = new ProducerMemoryMappedQueueOptions(
            InitializationTests.TestQueueName,
            InitializationTests.TestFileName,
            InitializationTests.TestQueueSize);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(
            InitializationTests.TestQueueName,
            InitializationTests.TestFileName,
            InitializationTests.TestQueueSize);
        using var producer = new Producer(producerOptions);
        using var consumer = new Consumer(consumerOptions);
        
        var messages = new[]
        {
            "First"u8.ToArray(),
            "Second"u8.ToArray(),
            "Third"u8.ToArray()
        };
        
        // Act
        foreach (var message in messages)
            producer.Enqueue(message);
        
        // Assert
        foreach (var message in messages)
        {
            var dequeueResult = consumer.Dequeue();
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
        var producerOptions = new ProducerMemoryMappedQueueOptions(
            InitializationTests.TestQueueName,
            InitializationTests.TestFileName,
            InitializationTests.TestQueueSize);
        using var producer = new Producer(producerOptions);
        
        // Act
        var result = producer.Enqueue(new byte[InitializationTests.TestQueueSize]);
        
        // Assert
        Assert.True(result.IsError);
        Assert.Equal(EnqueueErrorType.NotEnoughFreeMemory, result.Error);
    }
}
