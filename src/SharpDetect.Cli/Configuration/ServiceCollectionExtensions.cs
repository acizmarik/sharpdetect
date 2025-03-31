// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Plugins;
using SharpDetect.Loader;
using SharpDetect.Metadata;
using SharpDetect.PluginHost;
using SharpDetect.Reporting;
using SharpDetect.Serialization;

namespace SharpDetect.Cli.Configuration;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddAnalysisServices(
        this IServiceCollection services, 
        RunCommandArgs args, 
        Type pluginType)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(args.Analysis.LogLevel);
        });

        services.AddSharpDetectLoaderServices();
        services.AddSharpDetectMetadataServices();
        services.AddSharpDetectSerializationServices();
        services.AddSharpDetectReportingServices();
        services.AddSharpDetectPluginHostServices();
        services.AddSingleton(p => (IPlugin)ActivatorUtilities.CreateInstance(p, pluginType));
        services.AddSingleton(pluginType, p => p.GetRequiredService<IPlugin>());

        return services;
    }
}
