// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CliWrap;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Worker.Services;

public sealed class AnalysisWorker : IAnalysisWorker
{
    private static readonly TimeSpan ProfilerEventReceiveTimeout = TimeSpan.FromMilliseconds(50);
    private readonly RunCommandArgs _arguments;
    private readonly IPlugin _plugin;
    private readonly IPluginHost _pluginHost;
    private readonly IProfilerEventReceiver _eventReceiver;
    private readonly ILogger<AnalysisWorker> _logger;

    public AnalysisWorker(
        RunCommandArgs arguments,
        IPlugin plugin,
        IPluginHost pluginHost,
        IProfilerEventReceiver eventReceiver,
        ILogger<AnalysisWorker> logger)
    {
        _arguments = arguments;
        _plugin = plugin;
        _pluginHost = pluginHost;
        _eventReceiver = eventReceiver;
        _logger = logger;
    }

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var configurationPath = GetFullConfigurationPath();
        _logger.LogTrace("Running with arguments: {Arguments}.", _arguments);
        _logger.LogTrace("Configuration file: {ConfigFile}.", configurationPath);

        try
        {
            _plugin.Configuration.SerializeToFile(configurationPath);
            _logger.LogTrace("Serialized analyzed method descriptors into file: \"{Path}\".", configurationPath);

            var targetApplicationProcess = BuildTargetApplicationCommand().ExecuteAsync(cancellationToken);
            var rootPid = (uint)targetApplicationProcess.ProcessId;
            _logger.LogInformation("Started process with PID: {Pid}.", rootPid);

            ExecuteAnalysis(targetApplicationProcess.Task, rootPid, cancellationToken);
            _logger.LogInformation("Terminating analysis of process with PID: {Pid}.", rootPid);

            var commandResult = await targetApplicationProcess;
            if (commandResult.ExitCode != 0)
            {
                var level = _arguments.Target.Kind == TargetKind.TestAssembly ? LogLevel.Information : LogLevel.Warning;
                _logger.Log(level, "Target process exited with non-zero exit code: {ExitCode}.", commandResult.ExitCode);
            }
        }
        finally
        {
            CleanupConfigurationFile(configurationPath);
        }
    }
    
    private Command BuildTargetApplicationCommand()
    {
        var host = _arguments.Runtime.Host?.Path ?? "dotnet";
        var environmentVariables = BuildTargetEnvironmentVariables();
        var argsBuilder = TargetArgumentsBuilder.Build(_arguments, environmentVariables);

        var command = Cli.Wrap(host)
            .WithArguments(argsBuilder)
            .WithValidation(CommandResultValidation.None);

        if (!TargetArgumentsBuilder.RequiresEnvironmentInjection(_arguments))
        {
            command = command.WithEnvironmentVariables(builder =>
            {
                foreach (var (key, value) in environmentVariables)
                    builder.Set(key, value);
            });
        }

        if (_arguments.Target.WorkingDirectory is { } workingDirectory)
            command = command.WithWorkingDirectory(workingDirectory);

        var redirects = _arguments.Target.RedirectInputOutput;
        if (redirects != null)
        {
            if (redirects.SingleConsoleMode)
            {
                command = command.WithStandardInputPipe(PipeSource.FromStream(Console.OpenStandardInput()));
                command = command.WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()));
                command = command.WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()));
            }
            else
            {
                if (redirects?.StdinFilePath is { } stdin && stdin.Length > 0)
                    command = command.WithStandardInputPipe(PipeSource.FromFile(stdin));
                if (redirects?.StdoutFilePath is { } stdout && stdout.Length > 0)
                    command = command.WithStandardOutputPipe(PipeTarget.ToFile(stdout));
                if (redirects?.StderrFilePath is { } stderr && stderr.Length > 0)
                    command = command.WithStandardErrorPipe(PipeTarget.ToFile(stderr));
            }
        }

        return command;
    }

    private Dictionary<string, string> BuildTargetEnvironmentVariables()
    {
        var profilerPath = GetProfilerPath();
        var extension = Path.GetExtension(profilerPath);
        var profilerDirectory = Path.GetDirectoryName(profilerPath)!;
        var ipqPath = $"{Path.Combine(profilerDirectory, "SharpDetect.InterProcessQueue")}{extension}";

        var envVars = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CORECLR_ENABLE_PROFILING"] = "1",
            ["CORECLR_PROFILER"] = _arguments.Runtime.Profiler.Clsid.ToString(),
            ["CORECLR_PROFILER_PATH"] = profilerPath,
            ["SharpDetect_IPQ_PATH"] = ipqPath,
            ["SharpDetect_CONFIGURATION_PATH"] = GetFullConfigurationPath(),
            ["SharpDetect_LOG_LEVEL"] = ((int)_arguments.Runtime.Profiler.LogLevel).ToString()
        };

        foreach (var (key, value) in _arguments.Runtime.Host?.AdditionalEnvironmentVariables ?? Enumerable.Empty<KeyValuePair<string, string>>())
            envVars[key] = value;

        foreach (var (key, value) in _arguments.Target.AdditionalEnvironmentVariables ?? Enumerable.Empty<KeyValuePair<string, string>>())
            envVars[key] = value;

        return envVars;
    }

    private void ExecuteAnalysis(Task targetProcessTask, uint rootPid, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var currentEvent in ReceiveEvents(targetProcessTask, rootPid, cancellationToken))
            {
                if (currentEvent.EventArgs is ProfilerDestroyRecordedEvent && currentEvent.Metadata.Pid == rootPid)
                    return;

                if (_pluginHost.ProcessEvent(currentEvent) == RecordedEventState.Failed)
                {
                    LogFailureAndTerminateAnalysis();
                    return;
                }
            }
        }
        finally
        {
            if (_pluginHost is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private IEnumerable<RecordedEvent> ReceiveEvents(Task targetProcessTask, uint rootPid, CancellationToken cancellationToken)
    {
        // Phase 1: process is running
        while (!cancellationToken.IsCancellationRequested && !targetProcessTask.IsCompleted)
        {
            if (!_eventReceiver.TryReceiveNotification(ProfilerEventReceiveTimeout, out var currentEvent))
            {
                if (targetProcessTask.IsCompleted)
                    break;
                
                continue;
            }

            yield return currentEvent;
        }

        // Phase 2: process has exited. Ensure the ring buffer is drained.
        const int maxConsecutiveEmptyPolls = 4;
        var consecutiveEmptyPolls = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_eventReceiver.TryReceiveNotification(ProfilerEventReceiveTimeout, out var currentEvent))
            {
                if (++consecutiveEmptyPolls >= maxConsecutiveEmptyPolls)
                    break;
            }
            else
            {
                consecutiveEmptyPolls = 0;
                yield return currentEvent;

                if (currentEvent.EventArgs is ProfilerDestroyRecordedEvent && currentEvent.Metadata.Pid == rootPid)
                    yield break;
            }
        }
    }
    
    private string GetProfilerPath()
    {
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? _arguments.Runtime.Profiler.PathWindowsX64
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                    ? _arguments.Runtime.Profiler.PathLinuxArm64
                    : _arguments.Runtime.Profiler.PathLinuxX64
                : throw new PlatformNotSupportedException($"OS: {RuntimeInformation.OSDescription}.");
        
        if (path is null)
            throw new ArgumentException($"Profiler path for {RuntimeInformation.OSDescription} was not configured.");
        
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
    }
    
    private string GetFullConfigurationPath()
    {
        var tempFolder = _plugin.Configuration.TemporaryFilesFolder ?? Path.GetTempPath();
        return Path.Combine(tempFolder, PluginConfiguration.GetConfigurationFileName(_plugin.Configuration.SessionId));
    }

    private void CleanupConfigurationFile(string configurationPath)
    {
        if (!File.Exists(configurationPath))
            return;
        
        try
        {
            File.Delete(configurationPath);
            _logger.LogTrace("Deleted configuration file: \"{File}\".", configurationPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete configuration file: \"{File}\".", configurationPath);
        }
    }
    
    [DoesNotReturn]
    private void LogFailureAndTerminateAnalysis()
    {
        _logger.LogCritical("Cannot continue with analysis due to corrupted shadow runtime state.");
        throw new AnalysisFailedException();
    }
}