using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Plugins;
using SharpDetect.PluginHost.Services;

namespace SharpDetect.PluginHost;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectPluginHostServices(this IServiceCollection services)
    {
        services.AddSingleton<IPluginHost, Services.PluginHost>();
        services.AddSingleton<ICallstackResolver, CallstackResolver>();
        services.AddSingleton<IRecordedEventsDeliveryContext, RecordedEventsDeliveryContext>();
        services.AddSingleton<IRecordedEventBindingsCompiler, RecordedEventBindingsCompiler>();
    }
}
