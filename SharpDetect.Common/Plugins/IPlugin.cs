using Microsoft.Extensions.Configuration;

namespace SharpDetect.Common.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        Version Version { get; }

        void Initialize(IConfiguration configuration);
    }
}
