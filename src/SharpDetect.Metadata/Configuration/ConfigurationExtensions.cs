using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Services.Metadata;

namespace SharpDetect.Metadata.Configuration
{
    public static class ConfigurationExtensions
    {
        public static void AddMetadata(this IServiceCollection collection)
        {
            collection.AddSingleton<IMetadataContext, MetadataContext>();
        }
    }
}
