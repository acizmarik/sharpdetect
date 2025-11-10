// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

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
        _logger.LogTrace("Running with arguments: {Arguments}.", _arguments);
        
        _plugin.Configuration.SerializeToFile(GetFullConfigurationPath());
        _logger.LogTrace("Serialized analyzed method descriptors into file: \"{Path}\".", GetFullConfigurationPath());

        var targetApplicationProcess = BuildTargetApplicationCommand().ExecuteAsync();
        _logger.LogInformation("Started process with PID: {Pid}.", targetApplicationProcess.ProcessId);

        ExecuteAnalysis(targetApplicationProcess.Task, cancellationToken);
        _logger.LogInformation("Terminating analysis of process with PID: {Pid}.", targetApplicationProcess.ProcessId);

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
            
            if (ProcessEvent(currentEvent) == RecordedEventState.Failed)
            {
                _logger.LogCritical("Cannot continue with analysis due to previous errors.");
                return;
            }

            // Check if we unlocked some thread(s) and execute blocked events if possible
            if (_eventsDeliveryContext.HasUnblockedThreads())
            {
                foreach (var unblockedThreadId in _eventsDeliveryContext.ConsumeUnblockedThreads())
                {
                    foreach (var undeliveredEvent in _eventsDeliveryContext.ConsumeUndeliveredEvents(unblockedThreadId))
                    {
                        if (ProcessEvent(undeliveredEvent) != RecordedEventState.Executed)
                            break;
                    }
                }
            }

            previousRecordedEvent = currentEvent;
        }
    }
    
    private RecordedEventState ProcessEvent(RecordedEvent recordedEvent)
    {
        try
        {
            return _pluginHost.ProcessEvent(recordedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing event {Event}.", recordedEvent);
            return RecordedEventState.Failed;
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
    
    private static string GetFullConfigurationPath()
    {
        return Path.Combine(Path.GetTempPath(), PluginConfiguration.ConfigurationFileName);
    }
}