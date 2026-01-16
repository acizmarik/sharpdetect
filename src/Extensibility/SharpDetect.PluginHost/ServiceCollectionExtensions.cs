using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Plugins;
using SharpDetect.PluginHost.Services;

namespace SharpDetect.PluginHost;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectPluginHostServices(this IServiceCollection services)
    {
        services.AddSingleton<PluginHostFactory>();
        services.AddSingleton<ICallstackResolver, CallstackResolver>();
        services.AddSingleton<IRecordedEventsDeliveryContext, RecordedEventsDeliveryContext>();
        services.AddSingleton<IRecordedEventBindingsCompiler, RecordedEventBindingsCompiler>();
        services.AddSingleton<IPluginHost>(sp =>
        {
            var factory = sp.GetRequiredService<PluginHostFactory>();
            var plugin = sp.GetRequiredService<IPlugin>();
            return factory.CreateHost(plugin);
        });
    }
}
