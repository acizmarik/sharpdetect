using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using SharpDetect.Common;
using SharpDetect.Common.Services;
using SharpDetect.Console.Configuration;
using SharpDetect.Core.Configuration;
using System.CommandLine;

namespace SharpDetect.Console
{
    public partial class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = CreateConfiguration();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureCommonServices(services, configuration);
            ConfigureCliSpecificServices(services);
            var servicesProvider = services.BuildServiceProvider();

            // Execute root command
            var handler = servicesProvider.GetRequiredService<RootCommand>();
            await handler.InvokeAsync(args).ConfigureAwait(continueOnCapturedContext: false);
        }

        internal static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: false)
                .Build();
        }

        internal static void ConfigureCommonServices(ServiceCollection services, IConfiguration configuration)
        {
            // Register configuration provider as a service
            services.AddSingleton(configuration);

            // Register logging service
            services.AddLogging(builder =>
            {
                var eventFormat = configuration.GetRequiredSection(Constants.Serilog.Template).Value;
                var minimumLevelRaw = configuration.GetRequiredSection(Constants.Serilog.Level).Value;
                if (!Enum.TryParse<LogEventLevel>(minimumLevelRaw, out var minimumLevel))
                    throw new ArgumentException($"Provided setting {minimumLevel} is not valid!");

                var minimumLevelConfig = new Serilog.Core.LoggingLevelSwitch(minimumLevel);
                Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext().MinimumLevel.ControlledBy(minimumLevelConfig)
                    .WriteTo.Console(outputTemplate: eventFormat)
                    .CreateLogger();

                builder.AddSerilog(dispose: true);
            });

            // Add SharpDetect
            services.AddSharpDetectCore();
        }

        internal static void ConfigureCliSpecificServices(ServiceCollection services)
        {
            // Register command handlers
            services.AddCommandLineHandlers();

            // Make application run based on real time
            services.AddSingleton<IDateTimeProvider, UtcDateTimeProvider>();
        }
    }
}