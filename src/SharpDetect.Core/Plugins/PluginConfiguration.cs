// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using System.Text.Json;

namespace SharpDetect.Core.Plugins;

public record PluginConfiguration(
    uint EventMask,
    string SharedMemoryName,
    string? SharedMemoryFile,
    uint SharedMemorySize,
    string CommandQueueName,
    string? CommandQueueFile,
    uint CommandQueueSize,
    string? TemporaryFilesFolder,
    object? AdditionalData)
{
    public const string ConfigurationFileName = "SharpDetect_Configuration.json";
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static PluginConfiguration Create(
        COR_PRF_MONITOR eventMask,
        string? temporaryFilesFolder,
        object? additionalData)
    {
        var tempFolder = temporaryFilesFolder ?? Path.GetTempPath();
        return new PluginConfiguration(
            EventMask: (uint)eventMask,
            SharedMemoryName: "SharpDetect_NotificationsQueue",
            SharedMemoryFile: Path.Combine(tempFolder, "SharpDetect_NotificationsQueue.data"),
            SharedMemorySize: 20_971_520 /* 20 MB */,
            CommandQueueName: "SharpDetect_CommandQueue",
            CommandQueueFile: Path.Combine(tempFolder, "SharpDetect_CommandQueue.data"),
            CommandQueueSize: 1_048_576 /* 1 MB */,
            temporaryFilesFolder,
            additionalData);
    }

    public void SerializeToFile(string absolutePath)
    {
        try
        {
            using var fileStream = File.Open(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var serializedData = JsonSerializer.Serialize(new
            {
                EventMask,
                SharedMemoryName,
                SharedMemoryFile,
                SharedMemorySize,
                CommandQueueName,
                CommandQueueFile,
                CommandQueueSize,
                AdditionalData
            }, _jsonSerializerOptions);
            using var writer = new StreamWriter(fileStream);
            writer.Write(serializedData);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Could not serialize method descriptors.", e);
        }
    }
}