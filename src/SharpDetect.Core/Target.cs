using CliWrap;
using Microsoft.Extensions.Configuration;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Interop;

namespace SharpDetect.Core
{
    internal class Target
    {
        private readonly string ExecutablePath;
        private readonly string CommandLineArguments;
        private readonly string WorkingDirectory;
        private readonly Command CompiledCommand;

        public Target(IConfiguration configuration)
        {
            ExecutablePath = configuration.GetSection(Constants.Configuration.TargetAssembly).Get<string>();
            CommandLineArguments = configuration.GetSection(Constants.Configuration.CommandLineArgs).Get<string>() ?? string.Empty;
            Guard.NotNull<string, ArgumentException>(
                WorkingDirectory = configuration.GetSection(Constants.Configuration.WorkingDirectory).Get<string>()
                ?? Path.GetDirectoryName(ExecutablePath)!);

            // Flags
            var monitorFlags = configuration.GetSection(Constants.Profiling.Monitor).Get<string[]>();
            var enableFlags = configuration.GetSection(Constants.Profiling.Enable).Get<string[]>();
            var disableFlags = configuration.GetSection(Constants.Profiling.Disable).Get<string[]>();
            var flags = Enum.Parse<COR_PRF_MONITOR>(string.Join(',', monitorFlags.Concat(enableFlags).Concat(disableFlags)));

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

            CompiledCommand = Cli.Wrap("dotnet")
                .WithArguments($"run {ExecutablePath} \"{CommandLineArguments}\"")
                .WithWorkingDirectory(WorkingDirectory)
                .WithEnvironmentVariables(builder =>
                {
                    // Profiling flags
                    builder.Set("CORECLR_ENABLE_PROFILING", "1");
                    builder.Set("CORECLR_PROFILER", "{79d2f672-5206-11eb-ae93-0242ac130002}");
                    builder.Set("CORECLR_PROFILER_PATH", 
                        /* TODO: change this */ "D:/Workspace/Cpy/SharpDetect/src/SharpDetect.Profiler/build/bin/SharpDetect.Profiler.dll");

                    // CoreCLR knobs
                    builder.Set("COMPlus_ReadyToRun", "0");
                    builder.Set("COMPlus_TailCallOpt", "0");
                    builder.Set("COMPlus_TieredCompilation", "0");

                    // SharpDetect settings
                    builder.Set("SHARPDETECT_Profiler_Flags", ((ulong)flags).ToString());
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
        }

        public Task<CommandResult> ExecuteAsync(CancellationToken ct)
        {
            return CompiledCommand.ExecuteAsync(ct);
        }
    }
}
