// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Loader;
using SharpDetect.Loader.Services;

namespace SharpDetect.Loader;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectLoaderServices(this IServiceCollection services)
    {
        services.AddScoped<IAssemblyLoadContext, AssemblyLoadContext>();
        services.AddScoped<IModuleBindContext, ModuleBindContext>();
    }
}
