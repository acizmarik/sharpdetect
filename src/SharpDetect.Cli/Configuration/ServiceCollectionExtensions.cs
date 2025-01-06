// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Extensibility;
using SharpDetect.Extensibility.Configuration;
using SharpDetect.Loader.Configuration;
using SharpDetect.Metadata.Configuration;
using SharpDetect.Reporting.Configuration;
using SharpDetect.Serialization.Configuration;

namespace SharpDetect.Cli.Configuration
{
    internal static class ServiceCollectionExtensions
    {
        internal static IServiceCollection AddAnalysisServices<TPlugin>(this IServiceCollection services)
            where TPlugin : class, IPlugin
        {
            return AddAnalysisServices(services, typeof(TPlugin));
        }

        internal static IServiceCollection AddAnalysisServices(this IServiceCollection services, Type pluginType)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            services.AddSharpDetectLoaderServices();
            services.AddSharpDetectMetadataServices();
            services.AddSharpDetectSerializationServices();
            services.AddSharpDetectExtensibilityServices();
            services.AddSharpDetectReportingServices();
            services.AddSingleton(p => (IPlugin)ActivatorUtilities.CreateInstance(p, pluginType));
            services.AddSingleton<PluginProxy>();

            return services;
        }
    }
}
