using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Core;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;

namespace SharpDetect.Console.Commands
{
    internal class RunAnalysisCommand : CommandBase
    {
        public override ICommandHandler Handler { get; }
        public override IReadOnlyList<Argument> Arguments { get; }
        private AssemblyName? targetAssembly;

        public RunAnalysisCommand()
            : base(name: "analyze", description: "Perform dynamic analysis on a .NET application.")
        {
            Arguments = new List<Argument>()
            {
                new Argument<string>("localConfiguration", "Configuration describing the analysis"),
                new Argument<string>("pluginsConfiguration", "One or multiple plugins delimited with '|' character")
            };

            Handler = CommandHandler.Create(HandlerImplementationAsync);
        }

        private async Task<bool> HandlerImplementationAsync(string localConfiguration, string pluginsConfiguration)
        {
            ILogger? logger = null;

            try
            {
                ValidateLocalConfiguration(localConfiguration);
                ValidatePluginConfiguration(pluginsConfiguration);

                var configuration = Program.CreateConfiguration(
                    overridingYamlFile: localConfiguration, /* local configuration */
                    new KeyValuePair<string, string?>(Constants.Configuration.PluginsChain, pluginsConfiguration /* plugins configuration */));
                var serviceProvider = BuildServices(configuration);
                var analysis = serviceProvider.GetRequiredService<IAnalysis>();
                logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RunAnalysisCommand>();

                logger.LogInformation("[{class}] Target assembly: {assembly}", nameof(RunAnalysisCommand), targetAssembly);
                logger.LogInformation("[{class}] Requested plugins: {plugins}", nameof(RunAnalysisCommand), pluginsConfiguration);

                // Execution
                logger.LogDebug("[{class}] Execution started with {arguments}.", nameof(RunAnalysisCommand), new[] { localConfiguration, pluginsConfiguration });
                // TODO: implement CancellationTokenProvider
                var analysisResult = await analysis.ExecuteAnalysisAndTargetAsync(CancellationToken.None);

                // Results
                var reports = new List<ReportBase>();
                var reportsReader = serviceProvider.GetRequiredService<IReportsReaderProvider>();
                await foreach (var report in reportsReader.GetReportsReader().ReadAllAsync())
                    reports.Add(report);
                var reportsRenderer = serviceProvider.GetRequiredService<IReportsRenderer>();
                reportsRenderer.Render(reports);

                return analysisResult;
            }
            catch (Exception ex)
            {
                var errorFormat = "[{0}] Execution failed.";
                if (logger is not null)
                {
                    logger.LogError(ex, errorFormat, nameof(RunAnalysisCommand));
                }
                else
                {
                    // In case the service provider was not built yet
                    System.Console.Error.WriteLine(errorFormat, nameof(RunAnalysisCommand));
                    System.Console.Error.WriteLine(ex);
                }

                return await Task.FromResult(false);
            }
            finally
            {
                var messageFormat = "[{0}] Execution ended.";
                if (logger is not null)
                {
                    logger.LogDebug(messageFormat, nameof(RunAnalysisCommand));
                }
                else
                {
                    System.Console.WriteLine(messageFormat, nameof(RunAnalysisCommand));
                }
            }
        }

        private void ValidateLocalConfiguration(string localConfigPath)
        {
            // Ensure path is valid
            Guard.IsNotNullOrEmpty(localConfigPath);
            if (!File.Exists(localConfigPath))
                ThrowHelper.ThrowArgumentException($"File {localConfigPath} does not exist.");

            // Ensure configuration is valid
            var localConfig = new ConfigurationBuilder()
                .AddYamlFile(localConfigPath)
                .Build();

            // Target must be set and be a valid assembly
            var rawTargetAssembly = localConfig.GetRequiredSection(Constants.Configuration.TargetAssembly).Value;
            Guard.IsNotNullOrWhiteSpace(rawTargetAssembly, Constants.Configuration.TargetAssembly);
            try
            {
                // Attempt to load target assembly
                targetAssembly = AssemblyName.GetAssemblyName(rawTargetAssembly);
            }
            catch (Exception ex)
            {
                ThrowHelper.ThrowArgumentException($"Could not load target assembly {rawTargetAssembly}.", ex);
            }
        }

        private void ValidatePluginConfiguration(string pluginConfiguration)
        {
            // Ensure that plugins configuration is valid
            Guard.IsNotEmpty(pluginConfiguration);
        }
    }
}
