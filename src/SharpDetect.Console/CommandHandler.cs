using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Core;
using System.Reflection;

namespace SharpDetect.Console
{
    public class CommandHandler
    {
        private readonly IAnalysis analysis;
        private readonly IConfiguration configuration;
        private readonly ILogger<CommandHandler> logger;

        public CommandHandler(IAnalysis analysis, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this.analysis = analysis;
            this.configuration = configuration;
            this.logger = loggerFactory.CreateLogger<CommandHandler>();
        }

        public async Task<bool> RunAnalysisAsync(string localConfigFilePath, string pluginsConfiguration, CancellationToken ct)
        {
            logger.LogDebug("[{class}] Command {command} execution started with arguments {args}.", nameof(CommandHandler), nameof(RunAnalysisAsync),
                new[] { localConfigFilePath, pluginsConfiguration });

            try
            {
                // Prepare analysis configuration
                ParseLocalConfiguration(localConfigFilePath);
                ParsePluginsConfiguration(pluginsConfiguration);

                // Execute analysis
                return await analysis.ExecuteAnalysisAndTargetAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{class}] Command {command} failed.", nameof(CommandHandler), nameof(RunAnalysisAsync));
                return await Task.FromResult(false);
            }
            finally
            {
                logger.LogDebug("[{class}] Command {command} execution ended.", nameof(CommandHandler), nameof(RunAnalysisAsync));
            }
        }

        private void ParseLocalConfiguration(string localConfigFilePath)
        {
            var localConfig = new ConfigurationBuilder()
                .AddJsonFile(localConfigFilePath)
                .Build();

            // Ensure target assembly is set
            ThrowHelpers.ThrowIfNull<ArgumentNullException>(localConfig.GetRequiredSection(Constants.Configuration.TargetAssembly).Value);
            // Ensure target assembly exists
            var targetAssembly = localConfig.GetRequiredSection(Constants.Configuration.TargetAssembly).Value;
            ThrowHelpers.ThrowIf<FileNotFoundException>(!File.Exists(targetAssembly));
            // Ensure file is a valid assembly
            var assemblyName = AssemblyName.GetAssemblyName(targetAssembly);
            logger.LogInformation("[{class}] Target assembly: {analysis}", nameof(CommandHandler), assemblyName);

            // Propagate configuration
            foreach (var (key, value) in localConfig.AsEnumerable())
                configuration[key] = value;
        }

        private void ParsePluginsConfiguration(string pluginsConfiguration)
        {
            // Ensure that plugins configuration is valid
            ThrowHelpers.ThrowIfEmpty<char, ArgumentException>(pluginsConfiguration);
            logger.LogInformation("[{class}] Registered plugins: {plugins}", nameof(CommandHandler), pluginsConfiguration);

            // Propagate configuration
            configuration[Constants.Configuration.PluginsChain] = pluginsConfiguration;
        }
    }
}
