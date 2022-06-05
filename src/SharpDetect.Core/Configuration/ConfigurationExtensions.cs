using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Scripts;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Communication.Endpoints;
using SharpDetect.Core.Models;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Runtime.Monitoring;
using SharpDetect.Core.Scripts;
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
            services.AddInstrumentation();
            services.AddModuleDescriptors();

            services.AddSingleton<IDateTimeProvider, UtcDateTimeProvider>();
            services.AddSingleton<ILuaBridge, LuaBridge>();
            services.AddScoped<IAnalysis, Analysis>();
            services.AddScoped<RuntimeEventsHub>();      
            services.AddScoped<IProfilingMessageHub, ProfilingMessageHub>();
            services.AddScoped<IRewritingMessageHub, RewritingMessageHub>();
            services.AddScoped<IExecutingMessageHub, ExecutingMessageHub>();
            services.AddScoped<INotificationsConsumer, NotificationServer>();
            services.AddScoped<IRequestsProducer, RequestServer>();
            services.AddScoped<IProfilingClient, ProfilingClient>();
            services.AddScoped<IHealthMonitor, HealthMonitor>();
            services.AddScoped<IPluginsManager, PluginsManager>();

            services.AddScoped<IShadowExecutionObserver, RuntimeEventsHub>(p => p.GetRequiredService<RuntimeEventsHub>());
        }

        private static void AddModuleDescriptors(this IServiceCollection services)
        {
            services.AddScoped<IMethodDescriptorRegistry>(p =>
            {
                var configuration = p.GetRequiredService<IConfiguration>();
                var luaBridge = p.GetRequiredService<ILuaBridge>();

                var registry = new MethodDescriptorRegistry();
                foreach (var directory in luaBridge.ModuleDirectories)
                {
                    Guard.True<ArgumentException>(Directory.Exists(directory));
                    foreach (var file in Directory.GetFiles(directory, "*.lua"))
                    {
                        var script = luaBridge.LoadModuleAsync(file).Result;
                        var methods = new List<(MethodIdentifier, MethodInterpretationData)>();
                        var descriptor = luaBridge.CreateAssemblyDescriptor(script);

                        var name = descriptor.GetAssemblyName();
                        var isCoreLib = descriptor.IsCoreLibrary();
                        descriptor.GetMethodDescriptors(methods);

                        registry.Register(new LibraryDescriptor(name, isCoreLib, methods));
                    }
                }

                return registry;
            });
        }
    }
}
