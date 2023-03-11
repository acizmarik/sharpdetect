// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Dnlib.Extensions.Configuration;
using SharpDetect.Instrumentation.Injectors;
using SharpDetect.Instrumentation.SourceLinks;

namespace SharpDetect.Instrumentation.Configuration
{
    public static class ConfigurationExtensions
    {
        public static void AddInstrumentation(this IServiceCollection services)
        {
            services.AddStringHeapCache();
            services.AddScoped<IInstrumentor, Instrumentor>();
            services.AddScoped<IEventDescriptorRegistry, EventDescriptorRegistry>();
            services.AddInjectors();
        }

        private static void AddInjectors(this IServiceCollection services)
        {
            var injectors = typeof(ConfigurationExtensions).Assembly.GetTypes()
                .Where(x => !x.IsAbstract && x.IsClass && x.BaseType == typeof(InjectorBase));

            foreach (var injector in injectors)
                services.Add(new ServiceDescriptor(typeof(InjectorBase), injector, ServiceLifetime.Transient));
        }
    }
}
