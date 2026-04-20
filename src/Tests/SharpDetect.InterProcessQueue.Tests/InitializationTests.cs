using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Synchronization;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class InitializationTests : InterProcessQueueTestsBase
{
    public InitializationTests()
        : base(
            queueName: "SharpDetect_IPQ_Initialization_Test_Queue",
            queueFile: "SharpDetect_IPQ_Initialization_Test.data",
            size: 1024 * 1024,
            semaphoreName: "SHARPDETECT_IPQ_Initialization_Test_Semaphore")
    {
        
    }
    
    [Fact]
    public void InterProcessQueue_Initialize_CreatesEmpty()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize, TestSemaphoreName);
        using var producer = new Producer(producerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: true));
        using var consumer = new Consumer(consumerOptions, InterProcessSemaphore.CreateOrOpen(TestSemaphoreName, isOwner: false));
        
        // Act
        var result = consumer.TryDequeue();
        
        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Equal(DequeueErrorType.NothingToRead, result.Error);
    }
}