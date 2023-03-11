// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Services.Instrumentation;

namespace SharpDetect.Dnlib.Extensions.Configuration
{
    public static class ConfigurationExtensions
    {
        public static void AddStringHeapCache(this IServiceCollection services)
        {
            services.AddScoped<IStringHeapCache, StringHeapCache>();
        }
    }
}
