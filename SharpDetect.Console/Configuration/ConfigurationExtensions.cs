using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Services;
using SharpDetect.Core.Configuration;
using System.CommandLine;

namespace SharpDetect.Console.Configuration
{
    public static class ConfigurationExtensions
    {
        public static void AddCommandLineHandlers(this IServiceCollection services)
        {
            services.AddSharpDetectCore();
            services.AddSingleton<CommandHandler>();
            services.AddSingleton(provider =>
            {
                var rootCommand = new RootCommand("This is a dynamic analysis framework for .NET applications.");
                rootCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create(() => rootCommand.Invoke("-h"));

                var analysisCommand = new Command("analyze", "Perform dynamic analysis on a .NET application.")
                {
                    new Argument<string>("configuration", "Configuration describing the analysis"),
                    new Argument<string>("plugins", "One or multiple plugins delimited with '|' character")
                };

                var listCommand = new Command("plugins", "List all available plugins.");

                // TODO: implement CancellationTokenProvider service
                analysisCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create<string, string>(async (c, p) =>
                {
                    var handler = provider.GetRequiredService<CommandHandler>();
                    await handler.RunAnalysisAsync(c, p, CancellationToken.None);
                });
                rootCommand.AddCommand(analysisCommand);

                // TODO: implement CancellationTokenProvider service
                listCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create(async () =>
                {
                    var pluginsManager = provider.GetRequiredService<IPluginsManager>();
                    // Ensure plugins are loaded already
                    await pluginsManager.LoadPluginsAsync(CancellationToken.None);

                    System.Console.WriteLine("List of available plugins: ");
                    foreach (var plugin in pluginsManager.GetLoadedPluginInfos())
                        System.Console.WriteLine($"{plugin.Name}; Version={plugin.Version}; Path={plugin.FilePath}");
                });
                rootCommand.AddCommand(listCommand);

                return rootCommand;
            });
        }
    }
}
