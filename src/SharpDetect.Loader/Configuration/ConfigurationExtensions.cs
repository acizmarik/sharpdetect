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
