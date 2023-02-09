using CliWrap;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Interop;
using static SharpDetect.Common.Constants;

namespace SharpDetect.Core
{
    internal class Target
    {
        private readonly string profilerPath;
        private readonly string executablePath;
        private readonly string commandLineArguments;
        private readonly string workingDirectory;
        private readonly ILogger logger;
        private Command compiledCommand;

        public Target(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<Target>();
            var rawExecutablePath = configuration.GetSection(Constants.Configuration.TargetAssembly).Get<string>();
            var rawProfilerPath = configuration.GetSection(Constants.Configuration.ProfilerPath).Get<string>();
            Guard.IsNotNullOrWhiteSpace(rawExecutablePath);
            Guard.IsNotNullOrWhiteSpace(rawProfilerPath);

            if (!File.Exists(rawExecutablePath))
                throw new ArgumentException($"Target assembly with path {rawExecutablePath} does not exist.");
            if (!File.Exists(rawProfilerPath))
                throw new ArgumentException($"Profiler with path {rawProfilerPath} does not exist");
            executablePath = rawExecutablePath;
            profilerPath = rawProfilerPath;

            commandLineArguments = configuration.GetSection(Constants.Configuration.CommandLineArgs).Get<string>() ?? string.Empty;
            workingDirectory = configuration.GetSection(Constants.Configuration.WorkingDirectory).Get<string>() ?? Path.GetDirectoryName(executablePath)!;

            // Flags
            var monitorFlags = configuration.GetSection(Constants.Profiling.Monitor).Get<string[]>() ?? Array.Empty<string>();
            var enableFlags = configuration.GetSection(Constants.Profiling.Enable).Get<string[]>() ?? Array.Empty<string>();
            var disableFlags = configuration.GetSection(Constants.Profiling.Disable).Get<string[]>() ?? Array.Empty<string>();
            var flags = Enum.Parse<COR_PRF_MONITOR>(string.Join(',', monitorFlags.Concat(enableFlags).Concat(disableFlags)));

            // Signals endpoint
            var signalsPort = configuration.GetSection(Constants.Communication.Signals.Port).Get<string>();
            var signalsAddress = configuration.GetSection(Constants.Communication.Signals.Address).Get<string>();
            var signalsProtocol = configuration.GetSection(Constants.Communication.Signals.Protocol).Get<string>();
            // Notifications endpoint
            var notificationsPort = configuration.GetSection(Constants.Communication.Notifications.Port).Get<string>();
            var notificationsAddress = configuration.GetSection(Constants.Communication.Notifications.Address).Get<string>();
            var notificationsProtocol = configuration.GetSection(Constants.Communication.Notifications.Protocol).Get<string>();
            // Requests endpoint
            var requestsPort = configuration.GetSection(Constants.Communication.Requests.Outbound.Port).Get<string>();
            var requestsAddress = configuration.GetSection(Constants.Communication.Requests.Outbound.Address).Get<string>();
            var requestsProtocol = configuration.GetSection(Constants.Communication.Requests.Outbound.Protocol).Get<string>();
            // Reponses endpoint
            var responsesPort = configuration.GetSection(Constants.Communication.Requests.Inbound.Port).Get<string>();
            var responsesAddress = configuration.GetSection(Constants.Communication.Requests.Inbound.Address).Get<string>();
            var responsesProtocol = configuration.GetSection(Constants.Communication.Requests.Inbound.Protocol).Get<string>();

            compiledCommand = Cli.Wrap("dotnet")
                .WithArguments($"{executablePath} \"{commandLineArguments}\"")
                .WithWorkingDirectory(workingDirectory)
                .WithEnvironmentVariables(builder =>
                {
                    // Profiling flags
                    builder.Set("CORECLR_ENABLE_PROFILING", "1");
                    builder.Set("CORECLR_PROFILER", "{79d2f672-5206-11eb-ae93-0242ac130002}");
                    builder.Set("CORECLR_PROFILER_PATH", profilerPath);

                    // CoreCLR knobs
                    builder.Set("COMPlus_ReadyToRun", "0");
                    builder.Set("COMPlus_TailCallOpt", "0");
                    builder.Set("COMPlus_TieredCompilation", "0");

                    // SharpDetect settings
                    builder.Set("SHARPDETECT_Profiling_Flags", ((ulong)flags).ToString());
                    builder.Set("SHARPDETECT_Signals_Protocol", signalsProtocol);
                    builder.Set("SHARPDETECT_Signals_Address", signalsAddress);
                    builder.Set("SHARPDETECT_Signals_Port", signalsPort);
                    builder.Set("SHARPDETECT_Notifications_Protocol", notificationsProtocol);
                    builder.Set("SHARPDETECT_Notifications_Address", notificationsAddress);
                    builder.Set("SHARPDETECT_Notifications_Port", notificationsPort);
                    builder.Set("SHARPDETECT_Requests_Protocol", requestsProtocol);
                    builder.Set("SHARPDETECT_Requests_Address", requestsAddress);
                    builder.Set("SHARPDETECT_Requests_Port", requestsPort);
                    builder.Set("SHARPDETECT_Responses_Protocol", responsesProtocol);
                    builder.Set("SHARPDETECT_Responses_Address", responsesAddress);
                    builder.Set("SHARPDETECT_Responses_Port", responsesPort);
                })
                .WithValidation(CommandResultValidation.None);

            // IO redirections
            HandleOutputRedirection(configuration, "stdout", TargetAssemblyIO.Stdout.Redirect, TargetAssemblyIO.Stdout.File, compiledCommand.WithStandardOutputPipe);
            HandleOutputRedirection(configuration, "stderr", TargetAssemblyIO.Stderr.Redirect, TargetAssemblyIO.Stderr.File, compiledCommand.WithStandardErrorPipe);
            HandleInputRedirection(configuration, "stdin", TargetAssemblyIO.Stdin.Redirect, TargetAssemblyIO.Stdin.File, compiledCommand.WithStandardInputPipe);
        }

        private void HandleOutputRedirection(IConfiguration configuration, string name, string redirectKey, string fileKey, Func<PipeTarget, Command> registrator)
        {
            if (configuration.GetSection(redirectKey).Get<bool?>() is true)
            {
                if (configuration.GetSection(fileKey).Get<string?>() is not string path)
                {
                    path = $"{executablePath}-{name}.txt";
                    logger.LogWarning($"Requested redirection for {name} but file was not provided. Using {path} instead.");
                }
                compiledCommand = registrator(PipeTarget.ToFile(path));
            }
        }

        private void HandleInputRedirection(IConfiguration configuration, string name, string redirectKey, string fileKey, Func<PipeSource, Command> registrator)
        {
            if (configuration.GetSection(redirectKey).Get<bool?>() is true)
            {
                if (configuration.GetSection(fileKey).Get<string?>() is not string path)
                    throw new ArgumentException($"Requested redirection for {name} but file was not provided. It is a mandatory parameter when redirecting {name}.");

                compiledCommand = registrator(PipeSource.FromFile(path));
            }
        }

        public Task<CommandResult> ExecuteAsync(CancellationToken ct)
        {
            return compiledCommand.ExecuteAsync(ct);
        }
    }
}
