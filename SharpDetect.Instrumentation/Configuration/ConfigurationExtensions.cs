using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Dnlib.Extensions.Configuration;

namespace SharpDetect.Instrumentation.Configuration
{
    public static class ConfigurationExtensions
    {
        public static void AddInstrumentation(this IServiceCollection services)
        {
            services.AddStringHeapCache();
            services.AddScoped<IInstrumentor, Instrumentor>();
        }
    }
}
