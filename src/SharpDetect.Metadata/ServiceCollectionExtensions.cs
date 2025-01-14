// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Metadata;
using SharpDetect.Metadata.Services;

namespace SharpDetect.Metadata;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectMetadataServices(this IServiceCollection services)
    {
        services.AddScoped<IMetadataContext, MetadataContext>();
    }
}
