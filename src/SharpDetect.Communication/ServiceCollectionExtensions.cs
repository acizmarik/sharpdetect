// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Communication.Services;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Plugins;
using SharpDetect.InterProcessQueue.Configuration;

namespace SharpDetect.Communication;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectCommunicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IProfilerCommandSenderProvider, ProfilerCommandSenderProvider>();
        services.AddSingleton<IProfilerEventReceiver, ProfilerEventReceiver>();
        services.AddSingleton<ConsumerMemoryMappedQueueOptions>(provider =>
        {
            var plugin = provider.GetRequiredService<IPlugin>();
            return new ConsumerMemoryMappedQueueOptions(
                plugin.Configuration.SharedMemoryName,
                plugin.Configuration.SharedMemoryFile,
                plugin.Configuration.SharedMemorySize);
        });
    }
}