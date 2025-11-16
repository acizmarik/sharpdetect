using SharpDetect.InterProcessQueue.Configuration;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public class InitializationTests : InterProcessQueueTestsBase
{
    public InitializationTests()
        : base(
            queueName: "SharpDetect_IPQ_Initialization_Test_Queue",
            queueFile: "SharpDetect_IPQ_Initialization_Test.data",
            size: 1024 * 1024)
    {
        
    }
    
    [Fact]
    public void InterProcessQueue_Initialize_CreatesEmpty()
    {
        // Arrange
        var producerOptions = new ProducerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize);
        var consumerOptions = new ConsumerMemoryMappedQueueOptions(TestQueueName, TestFileName, TestQueueSize);
        using var producer = new Producer(producerOptions);
        using var consumer = new Consumer(consumerOptions);
        
        // Act
        var result = consumer.Dequeue();
        
        // Assert
        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Equal(DequeueErrorType.NothingToRead, result.Error);
    }
}