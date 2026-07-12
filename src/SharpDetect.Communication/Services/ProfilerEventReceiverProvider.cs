// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Communication;
using SharpDetect.InterProcessQueue.Configuration;

namespace SharpDetect.Communication.Services;

internal sealed class ProfilerEventReceiverProvider : IProfilerEventReceiverProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConsumerMemoryMappedQueueOptions _baseOptions;

    public ProfilerEventReceiverProvider(IServiceProvider serviceProvider, ConsumerMemoryMappedQueueOptions baseOptions)
    {
        _serviceProvider = serviceProvider;
        _baseOptions = baseOptions;
    }

    public IProfilerEventReceiver Create(uint pid)
    {
        var perPidOptions = new ConsumerMemoryMappedQueueOptions(
            $"{_baseOptions.Name}.{pid}",
            _baseOptions.File is null ? null : $"{_baseOptions.File}.{pid}",
            _baseOptions.Capacity,
            $"{_baseOptions.SemaphoreName}.{pid}");

        return ActivatorUtilities.CreateInstance<ProfilerEventReceiver>(_serviceProvider, perPidOptions);
    }
}
