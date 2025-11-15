// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Events;
using SharpDetect.Core.Serialization;
using SharpDetect.InterProcessQueue;
using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Memory;

namespace SharpDetect.Communication.Services;

internal sealed class ProfilerEventReceiver : IProfilerEventReceiver, IDisposable
{
    private readonly IRecordedEventParser _recordedEventParser;
    private readonly ILogger<IProfilerEventReceiver> _logger;
    private readonly Consumer _consumer;
    private readonly string? _queueFilePath;
    private bool _disposed;

    public ProfilerEventReceiver(
        ConsumerMemoryMappedQueueOptions options,
        IRecordedEventParser recordedEventParser,
        ILogger<IProfilerEventReceiver> logger)
    {
        _consumer = new Consumer(options, ArrayPool<byte>.Shared);
        _recordedEventParser = recordedEventParser;
        _logger = logger;
        _queueFilePath = options.File;
        
        _logger.LogInformation("Started event receiver of IPC queue with name: \"{Name}\", file: \"{File}\", capacity: {Capacity} bytes.",
            options.Name,
            options.File,
            options.Capacity);
    }
    
    public bool TryReceiveNotification([NotNullWhen(true)] out RecordedEvent? recordedEvent)
    {
        var result = _consumer.Dequeue();
        if (result.IsError)
        {
            recordedEvent = null;
            return false;
        }

        if (!TryParseEvent(result.Value, _recordedEventParser, out var parsedEvent))
        {
            recordedEvent = null;
            return false;
        }

        recordedEvent = parsedEvent;
        return true;
    }
    
    private bool TryParseEvent(
        ILocalMemory<byte> memory, 
        IRecordedEventParser parser, 
        [NotNullWhen(true)] out RecordedEvent? recordedEvent)
    {
        try
        {
            recordedEvent = parser.Parse(memory.GetLocalMemory());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while parsing recorded event.");
            recordedEvent = null;
            return false;
        }
        finally
        {
            (memory as IDisposable)?.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        _consumer.Dispose();
        
        if (_queueFilePath is not null && File.Exists(_queueFilePath))
        {
            try
            {
                File.Delete(_queueFilePath);
                _logger.LogInformation("Deleted IPC queue file: \"{File}\".", _queueFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete IPC queue file: \"{File}\".", _queueFilePath);
            }
        }
    }
}