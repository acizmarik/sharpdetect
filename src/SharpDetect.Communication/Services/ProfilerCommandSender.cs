// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Commands;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Serialization;
using SharpDetect.InterProcessQueue;
using SharpDetect.InterProcessQueue.Configuration;

namespace SharpDetect.Communication.Services;

internal sealed class ProfilerCommandSender : IProfilerCommandSender, IDisposable
{
    private static ulong LastAssignedCommandId;
    private readonly Producer _producer;
    private readonly IProfilerCommandSerializer _commandSerializer;
    private readonly ILogger<ProfilerCommandSender> _logger;
    private readonly uint _processId;
    private bool _disposed;
    
    public ProfilerCommandSender(
        ProducerMemoryMappedQueueOptions options,
        IProfilerCommandSerializer commandSerializer,
        ILogger<ProfilerCommandSender> logger)
    {
        _producer = new Producer(options);
        _commandSerializer = commandSerializer;
        _logger = logger;
        _processId = (uint)Environment.ProcessId;
        
        _logger.LogInformation("Started command sender of IPC queue with name: \"{Name}\", file: \"{File}\", capacity: {Capacity} bytes.",
            options.Name,
            options.File,
            options.Capacity);
    }

    public CommandId SendCommand(IProfilerCommandArgs commandArgs)
    {
        var commandId = Interlocked.Increment(ref LastAssignedCommandId);
        var command = new ProfilerCommand(new RecordedCommandMetadata(_processId, commandId), commandArgs);
        var serializedCommand = _commandSerializer.Serialize(command);
        
        var result = _producer.Enqueue(serializedCommand);
        if (!result.IsError)
            return new CommandId(commandId);
        
        _logger.LogError("Failed to send command {CommandType} to profiler. Error: {Error}", 
            commandArgs.GetType().Name,
            result.Error);
        
        throw new InvalidOperationException($"Failed to send command to profiler. Error: {result.Error}");
    }

    public bool TrySendCommand(
        IProfilerCommandArgs commandArgs,
        [NotNullWhen(true)] out CommandId? commandId)
    {
        var rawCommandId = Interlocked.Increment(ref LastAssignedCommandId);
        var command = new ProfilerCommand(new RecordedCommandMetadata(_processId, rawCommandId), commandArgs);
        var serializedCommand = _commandSerializer.Serialize(command);
            
        var result = _producer.Enqueue(serializedCommand);
        if (!result.IsError)
        {
            commandId = new CommandId(rawCommandId);
            return true;
        }
        
        _logger.LogWarning("Failed to send command {CommandType} to profiler: {Error}",
            commandArgs.GetType().Name,
            result.Error);

        commandId = null;
        return false;
    }

    public bool TrySendCommand(
        IProfilerCommandArgs commandArgs,
        TimeSpan timeout,
        [NotNullWhen(true)] out CommandId? commandId)
    {
        var rawCommandId = Interlocked.Increment(ref LastAssignedCommandId);
        var command = new ProfilerCommand(new RecordedCommandMetadata(_processId, rawCommandId), commandArgs);
        var serializedCommand = _commandSerializer.Serialize(command);
            
        var result = _producer.Enqueue(serializedCommand, timeout);
        if (!result.IsError)
        {
            commandId = new CommandId(rawCommandId);
            return true;
        }
        
        _logger.LogWarning("Failed to send command {CommandType} to profiler within {Timeout}. Error: {Error}",
            commandArgs.GetType().Name,
            timeout,
            result.Error);

        commandId = null;
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _producer.Dispose();
    }
}