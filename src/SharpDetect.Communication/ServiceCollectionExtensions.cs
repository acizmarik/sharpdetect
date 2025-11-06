// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Communication.Services;
using SharpDetect.Core.Communication;

namespace SharpDetect.Communication;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectCommunicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IProfilerCommandSenderProvider, ProfilerCommandSenderProvider>();
    }
}