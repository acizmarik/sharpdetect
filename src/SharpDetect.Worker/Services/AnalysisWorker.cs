// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CliWrap;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using SharpDetect.InterProcessQueue;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Worker.Services;

public sealed class AnalysisWorker : IAnalysisWorker
{
    private const int EventBufferCapacity = 1000;
    private static readonly TimeSpan IdlePollDelay = TimeSpan.FromMilliseconds(10);
    private const int MaxBatchPerReceiver = 256;
    private const int MaxConsecutiveEmptyPolls = 50;

    private readonly RunCommandArgs _arguments;
    private readonly IPlugin _plugin;
    private readonly IPluginHost _pluginHost;
    private readonly IProfilerEventReceiverProvider _eventReceiverProvider;
    private readonly RegistrationTable _registrationTable;
    private readonly ILogger<AnalysisWorker> _logger;

    public AnalysisWorker(
        RunCommandArgs arguments,
        IPlugin plugin,
        IPluginHost pluginHost,
        IProfilerEventReceiverProvider eventReceiverProvider,
        RegistrationTable registrationTable,
        ILogger<AnalysisWorker> logger)
    {
        _arguments = arguments;
        _plugin = plugin;
        _pluginHost = pluginHost;
        _eventReceiverProvider = eventReceiverProvider;
        _registrationTable = registrationTable;
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
                _logger.Log(
                    level,
                    "Target process exited with non-zero exit code: {ExitCode} (0x{ExitCodeHex:X8}).",
                    commandResult.ExitCode,
                    commandResult.ExitCode);
            }
        }
        finally
        {
            CleanupConfigurationFile(configurationPath);
            CleanupRegistrationQueueFile();
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
        using var receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var events = new BlockingCollection<RecordedEvent>(EventBufferCapacity);
        var producer = new Thread(() => ProduceEvents(events, targetProcessTask, rootPid, receiveCts.Token))
        {
            IsBackground = true,
            Name = "SharpDetect.EventReceiver"
        };
        producer.Start();

        try
        {
            ProcessEvents(events, rootPid, cancellationToken);
        }
        finally
        {
            receiveCts.Cancel();
            producer.Join();

            if (_pluginHost is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private void ProduceEvents(BlockingCollection<RecordedEvent> events, Task targetProcessTask, uint rootPid, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var currentEvent in ReceiveEvents(targetProcessTask, rootPid, cancellationToken))
                events.Add(currentEvent, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when the consumer finishes (or analysis is cancelled) while we wait for buffer capacity.
        }
        finally
        {
            events.CompleteAdding();
        }
    }

    private void ProcessEvents(BlockingCollection<RecordedEvent> events, uint rootPid, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var currentEvent in events.GetConsumingEnumerable(cancellationToken))
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
        catch (OperationCanceledException)
        {
            // Analysis cancelled while waiting for the next event.
        }
    }

    private IEnumerable<RecordedEvent> ReceiveEvents(Task targetProcessTask, uint rootPid, CancellationToken cancellationToken)
    {
        var receivers = new Dictionary<uint, IProfilerEventReceiver>();
        try
        {
            var consecutiveEmptyPolls = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                DiscoverProcesses(receivers);

                var receivedAny = false;
                foreach (var (pid, receiver) in receivers)
                {
                    if (pid == rootPid)
                        continue;

                    for (var i = 0; i < MaxBatchPerReceiver && receiver.TryReceiveNotification(out var childEvent); i++)
                    {
                        receivedAny = true;
                        yield return childEvent;
                    }
                }

                if (receivers.TryGetValue(rootPid, out var rootReceiver))
                {
                    for (var i = 0; i < MaxBatchPerReceiver && rootReceiver.TryReceiveNotification(out var rootEvent); i++)
                    {
                        receivedAny = true;
                        yield return rootEvent;

                        if (rootEvent.EventArgs is ProfilerDestroyRecordedEvent && rootEvent.Metadata.Pid == rootPid)
                            yield break;
                    }
                }

                if (receivedAny)
                {
                    consecutiveEmptyPolls = 0;
                    continue;
                }

                if (targetProcessTask.IsCompleted && ++consecutiveEmptyPolls >= MaxConsecutiveEmptyPolls)
                    yield break;

                Thread.Sleep(IdlePollDelay);
            }
        }
        finally
        {
            foreach (var receiver in receivers.Values)
                (receiver as IDisposable)?.Dispose();
        }
    }

    private void DiscoverProcesses(Dictionary<uint, IProfilerEventReceiver> receivers)
    {
        foreach (var pid in _registrationTable.DrainNewRegistrations())
        {
            if (receivers.ContainsKey(pid))
                continue;

            try
            {
                receivers[pid] = _eventReceiverProvider.Create(pid);
                _logger.LogTrace("Attached event receiver for PID {Pid}.", pid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to attach event receiver for PID {Pid}.", pid);
            }
        }
    }
    
    private string GetProfilerPath()
    {
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? _arguments.Runtime.Profiler.PathWindowsX64
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? _arguments.Runtime.Profiler.PathLinuxX64
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

    private void CleanupRegistrationQueueFile()
    {
        _registrationTable.Dispose();

        var registrationFile = _plugin.Configuration.RegistrationQueueFile;
        if (registrationFile is null || !File.Exists(registrationFile))
            return;

        try
        {
            File.Delete(registrationFile);
            _logger.LogTrace("Deleted registration queue file: \"{File}\".", registrationFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete registration queue file: \"{File}\".", registrationFile);
        }
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