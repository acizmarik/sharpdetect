// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Services.Metadata;

namespace SharpDetect.Loader.Configuration
{
    public static class ConfigurationExtensions
    {
        public static void AddLoader(this IServiceCollection services)
        {
            services.AddSingleton<AssemblyLoadContext>();
            services.AddScoped<IModuleBindContext, ModuleBindContext>();
            services.AddScoped<IMetadataResolversProvider>(p => p.GetRequiredService<AssemblyLoadContext>());
        }
    }
}
