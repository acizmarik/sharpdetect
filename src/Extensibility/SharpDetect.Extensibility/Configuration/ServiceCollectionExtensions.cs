// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using Microsoft.Extensions.DependencyInjection;

namespace SharpDetect.Extensibility.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSharpDetectExtensibilityServices(this IServiceCollection services)
        {
            services.AddSingleton<IEventsDeliveryContext, EventsDeliveryContext>();
        }
    }
}
