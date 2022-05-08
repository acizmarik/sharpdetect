using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Communication.Endpoints;
using SharpDetect.Core.Models;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Runtime.Monitoring;
using SharpDetect.Instrumentation.Configuration;
using SharpDetect.Loader.Configuration;
using SharpDetect.Metadata.Configuration;

namespace SharpDetect.Core.Configuration
{
    public static class ConfigurationExtensions
    {
        public static void AddSharpDetectCore(this IServiceCollection services)
        {
            services.AddLoader();
            services.AddMetadata();
            services.AddBuiltinLibraryDescriptors();
            services.AddInstrumentation();

            services.AddSingleton<IDateTimeProvider, UtcDateTimeProvider>();
            services.AddScoped<IAnalysis, Analysis>();
            services.AddScoped<RuntimeEventsHub>();      
            services.AddScoped<IProfilingMessageHub, ProfilingMessageHub>();
            services.AddScoped<IRewritingMessageHub, RewritingMessageHub>();
            services.AddScoped<IExecutingMessageHub, ExecutingMessageHub>();
            services.AddScoped<INotificationsConsumer, NotificationServer>();
            services.AddScoped<IRequestsProducer, RequestServer>();
            services.AddScoped<IProfilingClient, ProfilingClient>();
            services.AddScoped<IHealthMonitor, HealthMonitor>();

            services.AddScoped<IShadowExecutionObserver, RuntimeEventsHub>(p => p.GetRequiredService<RuntimeEventsHub>());
        }

        private static void AddBuiltinLibraryDescriptors(this IServiceCollection services)
        {
            var descriptors = typeof(ConfigurationExtensions).Assembly.GetTypes()
                .Where(t => t is not null && !t.IsAbstract && t.IsClass && t.IsAssignableTo(typeof(ILibraryDescriptor)))
                .Select(d => (Activator.CreateInstance(d) as ILibraryDescriptor)!)
                ?? Enumerable.Empty<ILibraryDescriptor>();

            services.AddScoped<IMethodDescriptorRegistry>(p =>
            {
                var registry = new MethodDescriptorRegistry();
                foreach (var library in descriptors)
                    registry.Register(library);

                return registry;
            });
        }
    }
}
