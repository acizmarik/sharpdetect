// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Communication;
using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Synchronization;

namespace SharpDetect.Communication.Services;

internal sealed class ProfilerCommandSenderProvider : IProfilerCommandSenderProvider
{
    private readonly IServiceProvider _serviceProvider;

    public ProfilerCommandSenderProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IProfilerCommandSender Create(string ipcQueueName, string? ipcQueueFileName, uint size, string semaphoreName)
    {
        var semaphore = InterProcessSemaphore.CreateOrOpen(semaphoreName, isOwner: true);
        try
        {
            return ActivatorUtilities.CreateInstance<ProfilerCommandSender>(
                _serviceProvider,
                new ProducerMemoryMappedQueueOptions(ipcQueueName, ipcQueueFileName, size, semaphoreName),
                semaphore);
        }
        catch (Exception)
        {
            semaphore.Dispose();
            throw;
        }
    }
}
