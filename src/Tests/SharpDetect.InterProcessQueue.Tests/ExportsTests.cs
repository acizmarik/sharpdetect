// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.FFI;
using Xunit;

namespace SharpDetect.InterProcessQueue.Tests;

public unsafe class ExportsTests : InterProcessQueueTestsBase
{
    public ExportsTests()
        : base(
            queueName: "SharpDetect_IPQ_Exports_Test_Queue",
            queueFile: "SharpDetect_IPQ_Exports_Test.data",
            size: 1024 * 1024,
            semaphoreName: "SHARPDETECT_IPQ_Exports_Test_Semaphore")
    {

    }

    [Fact]
    public void Exports_Enqueue_AfterDestroy_ReturnsUnavailableInsteadOfThrowing()
    {
        // Arrange
        var producerHandle = Exports.CreateProducerImpl(TestQueueName, TestFileName, TestSemaphoreName, TestQueueSize);
        Assert.NotEqual(0, producerHandle);
        var message = "Hello, World!"u8.ToArray();

        fixed (byte* messagePtr = message)
        {
            var enqueueResultBeforeDestroy = Exports.EnqueueImpl(producerHandle, messagePtr, message.Length);
            Assert.Equal(EnqueueErrorType.OK, enqueueResultBeforeDestroy);

            // Act
            Exports.DestroyProducerImpl(producerHandle);
            var enqueueResultAfterDestroy = Exports.EnqueueImpl(producerHandle, messagePtr, message.Length);

            // Assert
            Assert.Equal(EnqueueErrorType.Unavailable, enqueueResultAfterDestroy);
        }
    }

    [Fact]
    public void Exports_DestroyProducer_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var producerHandle = Exports.CreateProducerImpl(TestQueueName, TestFileName, TestSemaphoreName, TestQueueSize);
        Assert.NotEqual(0, producerHandle);

        // Act & Assert (no exception)
        Exports.DestroyProducerImpl(producerHandle);
        Exports.DestroyProducerImpl(producerHandle);
    }

    [Fact]
    public void Exports_Dequeue_AfterDestroy_ReturnsUnavailableInsteadOfThrowing()
    {
        // Arrange
        var consumerHandle = Exports.CreateConsumerImpl(TestQueueName, TestFileName, TestSemaphoreName, TestQueueSize);
        Assert.NotEqual(0, consumerHandle);

        byte* dataPtr = null;
        var size = 0;

        // Act
        Exports.DestroyConsumerImpl(consumerHandle);
        var dequeueResult = Exports.DequeueImpl(consumerHandle, &dataPtr, &size);
        var dequeueWithTimeoutResult = Exports.DequeueWithTimeoutImpl(consumerHandle, &dataPtr, &size, timeoutMs: 1);

        // Assert
        Assert.Equal(DequeueErrorType.Unavailable, dequeueResult);
        Assert.Equal(DequeueErrorType.Unavailable, dequeueWithTimeoutResult);
    }

    [Fact]
    public void Exports_Enqueue_UnknownHandle_ReturnsUnavailable()
    {
        var message = "Hello, World!"u8.ToArray();
        fixed (byte* messagePtr = message)
        {
            var result = Exports.EnqueueImpl(producerHandle: 123456789, messagePtr, message.Length);
            Assert.Equal(EnqueueErrorType.Unavailable, result);
        }
    }

    [Fact]
    public void Exports_EnqueueDequeue_RoundTrip_Succeeds()
    {
        // Arrange
        var producerHandle = Exports.CreateProducerImpl(TestQueueName, TestFileName, TestSemaphoreName, TestQueueSize);
        var consumerHandle = Exports.CreateConsumerImpl(TestQueueName, TestFileName, TestSemaphoreName, TestQueueSize);
        Assert.NotEqual(0, producerHandle);
        Assert.NotEqual(0, consumerHandle);

        try
        {
            var message = "Test Message"u8.ToArray();

            fixed (byte* messagePtr = message)
            {
                // Act
                var enqueueResult = Exports.EnqueueImpl(producerHandle, messagePtr, message.Length);
                Assert.Equal(EnqueueErrorType.OK, enqueueResult);

                byte* dataPtr = null;
                var size = 0;
                var dequeueResult = Exports.DequeueImpl(consumerHandle, &dataPtr, &size);

                // Assert
                Assert.Equal(DequeueErrorType.OK, dequeueResult);
                Assert.Equal(message.Length, size);
                Assert.True(new ReadOnlySpan<byte>(dataPtr, size).SequenceEqual(message));
                Exports.FreeMemoryImpl(dataPtr);
            }
        }
        finally
        {
            Exports.DestroyProducerImpl(producerHandle);
            Exports.DestroyConsumerImpl(consumerHandle);
        }
    }
}
