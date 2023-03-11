// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Services.Metadata;

namespace SharpDetect.Metadata.Configuration
{
    public static class ConfigurationExtensions
    {
        public static void AddMetadata(this IServiceCollection collection)
        {
            collection.AddScoped<IMetadataContext, MetadataContext>();
        }
    }
}
