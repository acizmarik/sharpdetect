using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using SharpDetect.Common;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Console.Commands;
using SharpDetect.Console.Services;
using SharpDetect.Core.Configuration;
using System.CommandLine;

namespace SharpDetect.Console
{
    public partial class Program
    {
        public static async Task Main(string[] args)
        {
            // Execute root command
            var rootCommand = CreateCliRootCommand();
            await rootCommand.InvokeAsync(args).ConfigureAwait(continueOnCapturedContext: false);
        }

        internal static IConfiguration CreateConfiguration(
            string? overridingYamlFile = null, 
            params KeyValuePair<string, string?>[] inMemoryConfig)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: false);

            builder.AddInMemoryCollection(inMemoryConfig);

            if (overridingYamlFile is not null)
            {
                if (!File.Exists(overridingYamlFile))
                    throw new FileNotFoundException("Local configuration was not found.", overridingYamlFile);
                builder.AddYamlFile(overridingYamlFile, optional: false, reloadOnChange: false);
            }

            return builder.Build();
        }

        internal static void ConfigureCommonServices(ServiceCollection services, IConfiguration configuration)
        {
            // Register configuration provider as a service
            services.AddSingleton(configuration);

            // Register logging service
            services.AddLogging(builder =>
            {
                var eventFormat = configuration.GetRequiredSection(Constants.Serilog.Template).Value;
                var minimumLevelRaw = configuration.GetRequiredSection(Constants.Serilog.MinimumLevel).Value;

                Guard.IsNotNullOrEmpty(eventFormat, Constants.Serilog.Template);
                if (!Enum.TryParse<LogEventLevel>(minimumLevelRaw, out var minimumLevel))
                    throw new ArgumentException($"Provided setting {minimumLevel} is not valid.");

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
            // Make application run based on real time
            services.AddSingleton<IDateTimeProvider, UtcDateTimeProvider>();
            services.AddSingleton<IReportsRenderer, ConsoleReportsRenderer>();
        }

        private static RootCommand CreateCliRootCommand()
        {
            var rootCommand = new RootCommand("This is a dynamic analysis framework for .NET applications.");
            rootCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create(() => rootCommand.Invoke("-h"));

            foreach (var commandDescriptor in typeof(Program).Module.GetTypes().Where(t => t.IsAssignableTo(typeof(CommandBase)) && !t.IsAbstract))
            {
                var instance = (CommandBase)Activator.CreateInstance(commandDescriptor)!;
                var command = new Command(instance.Name, instance.Description);
                foreach (var arg in instance.Arguments)
                    command.AddArgument(arg);
                command.Handler = instance.Handler;
                rootCommand.Add(command);
            }

            return rootCommand;
        }
    }
}