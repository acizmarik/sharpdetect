// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Loaders;

namespace SharpDetect.Loader.Configuration;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectLoaderServices(this IServiceCollection services)
    {
        services.AddScoped<IAssemblyLoadContext, AssemblyLoadContext>();
        services.AddScoped<IModuleBindContext, ModuleBindContext>();
    }
}
