// Copyright 2025 Andrej Čižmárik and Contributors
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
    private readonly RunCommandArgs _arguments;
    private readonly IPlugin _plugin;
    private readonly IPluginHost _pluginHost;
    private readonly IProfilerEventReceiver _eventReceiver;
    private readonly IRecordedEventsDeliveryContext _eventsDeliveryContext;
    private readonly ILogger<AnalysisWorker> _logger;

    public AnalysisWorker(
        RunCommandArgs arguments,
        IPlugin plugin,
        IPluginHost pluginHost,
        IProfilerEventReceiver eventReceiver,
        IRecordedEventsDeliveryContext eventsDeliveryContext,
        ILogger<AnalysisWorker> logger)
    {
        _arguments = arguments;
        _plugin = plugin;
        _pluginHost = pluginHost;
        _eventReceiver = eventReceiver;
        _eventsDeliveryContext = eventsDeliveryContext;
        _logger = logger;
    }

    public ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var configurationPath = GetFullConfigurationPath();
        _logger.LogTrace("Running with arguments: {Arguments}.", _arguments);
        _logger.LogTrace("Configuration file: {ConfigFile}.", configurationPath);

        try
        {
            _plugin.Configuration.SerializeToFile(configurationPath);
            _logger.LogTrace("Serialized analyzed method descriptors into file: \"{Path}\".", configurationPath);

            var targetApplicationProcess = BuildTargetApplicationCommand().ExecuteAsync(cancellationToken);
            _logger.LogInformation("Started process with PID: {Pid}.", targetApplicationProcess.ProcessId);

            ExecuteAnalysis(targetApplicationProcess.Task, cancellationToken);
            _logger.LogInformation("Terminating analysis of process with PID: {Pid}.", targetApplicationProcess.ProcessId);
        }
        finally
        {
            CleanupConfigurationFile(configurationPath);
        }
        
        return ValueTask.CompletedTask;
    }
    
    private Command BuildTargetApplicationCommand()
    {
        var host = _arguments.Runtime.Host?.Path ?? "dotnet";
        var argsBuilder = new List<string>(capacity: 3);
        if (_arguments.Runtime.Host?.Args is { } hostArgs)
            argsBuilder.Add(hostArgs);
        argsBuilder.Add(_arguments.Target.Path);
        if (_arguments.Target.Args is { } targetArgs)
            argsBuilder.Add(targetArgs);

        var profilerPath = GetProfilerPath();
        var extension = Path.GetExtension(profilerPath);
        var profilerDirectory = Path.GetDirectoryName(profilerPath)!;
        var ipqPath = $"{Path.Combine(profilerDirectory, "SharpDetect.InterProcessQueue")}{extension}";

        var command = Cli.Wrap(host)
            .WithArguments(argsBuilder)
            .WithEnvironmentVariables(builder =>
            {
                builder.Set("CORECLR_ENABLE_PROFILING", "1");
                builder.Set("CORECLR_PROFILER", _arguments.Runtime.Profiler.Clsid.ToString());
                builder.Set("CORECLR_PROFILER_PATH", profilerPath);
                builder.Set("SharpDetect_IPQ_PATH", ipqPath);
                builder.Set("SharpDetect_CONFIGURATION_PATH", GetFullConfigurationPath());
                builder.Set("SharpDetect_LOG_LEVEL", ((int)_arguments.Runtime.Profiler.LogLevel).ToString());

                foreach (var (key, value) in _arguments.Runtime.Host?.AdditionalEnvironmentVariables ?? Enumerable.Empty<KeyValuePair<string, string>>())
                    builder.Set(key, value);
                
                foreach (var (key, value) in _arguments.Target.AdditionalEnvironmentVariables ?? Enumerable.Empty<KeyValuePair<string, string>>())
                    builder.Set(key, value);
            })
            .WithValidation(CommandResultValidation.None);

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
    
    private void ExecuteAnalysis(Task targetProcessTask, CancellationToken cancellationToken)
    {
        RecordedEvent? previousRecordedEvent = null;
        while (!ShouldTerminateAnalysis(targetProcessTask, previousRecordedEvent, cancellationToken))
        {
            if (!_eventReceiver.TryReceiveNotification(out var currentEvent))
            {
                previousRecordedEvent = null;
                continue;
            }
            
            if (_pluginHost.ProcessEvent(currentEvent) == RecordedEventState.Failed)
            {
                LogFailureAndTerminateAnalysis();
                return;
            }

            // Check if we unlocked some thread(s) and execute blocked events if possible
            if (_eventsDeliveryContext.HasUnblockedThreads())
            {
                foreach (var unblockedThreadId in _eventsDeliveryContext.ConsumeUnblockedThreads())
                {
                    foreach (var undeliveredEvent in _eventsDeliveryContext.ConsumeUndeliveredEvents(unblockedThreadId))
                    {
                        var processingResult = _pluginHost.ProcessEvent(undeliveredEvent);
                        if (processingResult == RecordedEventState.Failed)
                        {
                            LogFailureAndTerminateAnalysis();
                            return;
                        }

                        if (processingResult != RecordedEventState.Executed)
                        {
                            // Continue with the next thread
                            break;
                        }
                    }
                }
            }

            previousRecordedEvent = currentEvent;
        }
    }
    
    private static bool ShouldTerminateAnalysis(
        Task targetProcessTask,
        RecordedEvent? recordedEvents,
        CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested ||
               (targetProcessTask.IsCompleted && recordedEvents == null) || 
               (recordedEvents?.EventArgs is ProfilerDestroyRecordedEvent);
    }
    
    private string GetProfilerPath()
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException($"Architecture: {RuntimeInformation.ProcessArchitecture}.");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return _arguments.Runtime.Profiler.Path.WindowsX64;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return _arguments.Runtime.Profiler.Path.LinuxX64;
        else
            throw new PlatformNotSupportedException($"OS: {RuntimeInformation.OSDescription}.");
    }
    
    private string GetFullConfigurationPath()
    {
        var tempFolder = _plugin.Configuration.TemporaryFilesFolder ?? Path.GetTempPath();
        return Path.Combine(tempFolder, PluginConfiguration.ConfigurationFileName);
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