// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Events;
using SharpDetect.Core.Serialization;
using SharpDetect.InterProcessQueue;
using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Memory;
using SharpDetect.InterProcessQueue.Synchronization;

namespace SharpDetect.Communication.Services;

internal sealed class ProfilerEventReceiver : IProfilerEventReceiver, IDisposable
{
    private readonly EventBatchReader _reader;
    private readonly ILogger<IProfilerEventReceiver> _logger;
    private readonly Consumer _consumer;
    private readonly string? _queueFilePath;
    private QueueMessage _pendingMessage;
    private bool _hasPendingMessage;
    private bool _disposed;

    public ProfilerEventReceiver(
        ConsumerMemoryMappedQueueOptions options,
        IRecordedEventParser recordedEventParser,
        ILogger<IProfilerEventReceiver> logger)
    {
        var semaphore = InterProcessSemaphore.CreateOrOpen(options.SemaphoreName, isOwner: true);
        _consumer = new Consumer(options, semaphore, ArrayPool<byte>.Shared);
        _reader = new EventBatchReader(recordedEventParser);
        _logger = logger;
        _queueFilePath = options.File;

        _logger.LogInformation("Started event receiver of IPC queue with name: \"{Name}\", file: \"{File}\", capacity: {Capacity} bytes.",
            options.Name,
            options.File,
            options.Capacity);
    }

    public int TryReceiveNotifications(Span<RecordedEvent> destination, out int failedRecordsCount)
    {
        ArgumentOutOfRangeException.ThrowIfZero(destination.Length);
        failedRecordsCount = 0;

        if (!_hasPendingMessage && !TryDequeueMessage())
            return 0;

        var result = _reader.ReadInto(destination);
        if (!_reader.HasPendingRecords)
            ReleasePendingMessage();

        if (result.Corrupted)
            _logger.LogError("Discarding the remainder of a malformed profiler event batch.");

        if (result.FailedRecords > 0)
        {
            failedRecordsCount = result.FailedRecords;
            _logger.LogError(
                result.LastFailure,
                "Discarded {FailedRecords} unparsable profiler event(s); the most recent failure is attached.",
                result.FailedRecords);
        }

        return result.Count;
    }

    private bool TryDequeueMessage()
    {
        var result = _consumer.TryDequeue();
        if (result.IsError)
            return false;

        _pendingMessage = result.Value;
        _hasPendingMessage = true;
        _reader.SetBatch(_pendingMessage.Memory);
        return true;
    }

    private void ReleasePendingMessage()
    {
        if (!_hasPendingMessage)
            return;

        var message = _pendingMessage;
        _pendingMessage = default;
        _hasPendingMessage = false;

        _reader.Reset();
        message.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ReleasePendingMessage();
        _consumer.Dispose();

        if (_queueFilePath is not null && File.Exists(_queueFilePath))
        {
            try
            {
                File.Delete(_queueFilePath);
                _logger.LogTrace("Deleted IPC queue file: \"{File}\".", _queueFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete IPC queue file: \"{File}\".", _queueFilePath);
            }
        }
    }
}
