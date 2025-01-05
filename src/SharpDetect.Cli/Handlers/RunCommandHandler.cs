// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using CliFx.Infrastructure;
using CliWrap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Cli.Configuration;
using SharpDetect.Events;
using SharpDetect.Extensibility;
using SharpDetect.Extensibility.Abstractions;
using SharpDetect.InterProcessQueue;
using SharpDetect.InterProcessQueue.Configuration;
using SharpDetect.InterProcessQueue.Memory;
using SharpDetect.Serialization;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpDetect.Cli.Handlers
{
    internal sealed class RunCommandHandler
    {
        private const string RewritingConfigurationFileName = "SharpDetect_Rewriting_Config.data";
        private const string SharedMemoryName = "SharpDetect_Run_SharedMemory.data";
        private const int SharedMemorySize = 2_000_000;
        private static readonly string RewritingConfigurationPath = Path.Combine(Path.GetTempPath(), RewritingConfigurationFileName);

        public IServiceProvider ServiceProvider { get; }
        public RunCommandArgs Args { get; }
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly JsonSerializerOptions _jsonDeserializerOptions;
        private readonly PluginProxy _proxy;
        private readonly IEventsDeliveryContext _eventsDeliveryContext;
        private readonly IPlugin _plugin;
        private readonly ILogger _logger;

        public RunCommandHandler(string configurationFilePath)
        {
            _jsonDeserializerOptions = new JsonSerializerOptions()
            {
                Converters = { new JsonStringEnumConverter() },
                PropertyNamingPolicy = null
            };
            _jsonSerializerOptions = new JsonSerializerOptions() 
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            Args = LoadCommandArguments(configurationFilePath);
            ThrowOnInvalidConfiguration();

            var _pluginType = LoadPluginInfo();
            ServiceProvider = new ServiceCollection()
                .AddAnalysisServices(_pluginType)
                .BuildServiceProvider();

            _proxy = ServiceProvider.GetRequiredService<PluginProxy>();
            _plugin = ServiceProvider.GetRequiredService<IPlugin>();
            _eventsDeliveryContext = ServiceProvider.GetRequiredService<IEventsDeliveryContext>();
            _logger = ServiceProvider.GetRequiredService<ILogger<RunCommandHandler>>();
        }

        public ValueTask ExecuteAsync(IConsole _)
        {
            SerializeRewritingConfiguration();
            _logger.LogTrace("Serialized analyzed method descriptors into file: \"{Path}\".", RewritingConfigurationPath);

            var targetApplicationProcess = BuildTargetApplicationCommand().ExecuteAsync();
            _logger.LogInformation("Started process with PID: {Pid}.", targetApplicationProcess.ProcessId);

            var consumerOptions = new ConsumerMemoryMappedQueueOptions(SharedMemoryName, file: null, SharedMemorySize);
            using var eventConsumer = new Consumer(consumerOptions, ArrayPool<byte>.Shared);
            _logger.LogTrace("Started event consumer of IPC queue with name: \"{Name}\".", consumerOptions.Name);

            var eventParser = ServiceProvider.GetRequiredService<IRecordedEventParser>();
            ExecuteAnalysis(eventConsumer, targetApplicationProcess.Task, eventParser);
            _logger.LogInformation("Terminating analysis of process with PID: {Pid}.", targetApplicationProcess.ProcessId);

            return ValueTask.CompletedTask;
        }

        private void ExecuteAnalysis(Consumer consumer, Task targetProcessTask, IRecordedEventParser eventParser)
        {
            RecordedEvent? previousRecordedEvent = null;
            while (!ShouldTerminateAnalysis(targetProcessTask, previousRecordedEvent))
            {
                var result = consumer.Dequeue();
                if (result.IsError)
                {
                    previousRecordedEvent = null;
                    continue;
                }

                // Execute current event
                if (!TryParseEvent(result.Value, eventParser, out var currentEvent) || TryProcessEvent(currentEvent) == EventState.Failed)
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
                            if (TryProcessEvent(undeliveredEvent) != EventState.Executed)
                                break;
                        }
                    }
                }

                previousRecordedEvent = currentEvent;
            }
        }

        private bool TryParseEvent(
            ILocalMemory<byte> memory, 
            IRecordedEventParser parser, 
            [NotNullWhen(true)] out RecordedEvent? recordedEvent)
        {
            try
            {
                recordedEvent = parser.Parse(memory.GetLocalMemory());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while parsing recorded event.");
                recordedEvent = null;
                return false;
            }
            finally
            {
                (memory as IDisposable)?.Dispose();
            }
        }

        private EventState TryProcessEvent(RecordedEvent recordedEvent)
        {
            try
            {
                return _proxy.RelayEvent(recordedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing event {Event}.", recordedEvent);
                return EventState.Failed;
            }
        }

        private static bool ShouldTerminateAnalysis(Task targetProcessTask, RecordedEvent? recordedEvents)
        {
            return (targetProcessTask.IsCompleted && recordedEvents == null) || 
                   (recordedEvents?.EventArgs is ProfilerDestroyRecordedEvent);
        }

        private Command BuildTargetApplicationCommand()
        {
            var host = Args.Runtime.Host?.Path ?? "dotnet";
            var argsBuilder = new List<string>(capacity: 3);
            if (Args.Runtime.Host?.Args is { } hostArgs)
                argsBuilder.Add(hostArgs);
            argsBuilder.Add(Args.Target.Path);
            if (Args.Target.Args is { } targetArgs)
                argsBuilder.Add(targetArgs);

            var command = CliWrap.Cli.Wrap(host)
                .WithArguments(argsBuilder)
                .WithEnvironmentVariables(builder =>
                {
                    builder.Set("CORECLR_ENABLE_PROFILING", "1");
                    builder.Set("CORECLR_PROFILER", Args.Runtime.Profiler.Clsid.ToString());
                    builder.Set("CORECLR_PROFILER_PATH", Args.Runtime.Profiler.Path);
                    builder.Set("SharpDetect_PROF_EVENTMASK",
                        Args.Runtime.Profiler.EventMask.Cast<uint>().Aggregate((c, n) => c | n).ToString());
                    builder.Set("SharpDetect_COLLECT_FULL_STACKTRACES", Args.Runtime.Profiler.CollectFullStackTraces ? "1" : "0");
                    builder.Set("SharpDetect_REWRITING_CONFIGURATION_FILE_PATH", RewritingConfigurationPath);
                    builder.Set("SharpDetect_SHAREDMEMORY_NAME", SharedMemoryName);
                    builder.Set("SharpDetect_SHAREDMEMORY_SIZE", SharedMemorySize.ToString());

                    foreach (var (key, value) in Args.Runtime.Host?.AdditionalEnvironmentVariables ?? Enumerable.Empty<KeyValuePair<string, string>>())
                        builder.Set(key, value);
                    
                    foreach (var (key, value) in Args.Target.AdditionalEnvironmentVariables ?? Enumerable.Empty<KeyValuePair<string, string>>())
                        builder.Set(key, value);
                })
                .WithValidation(CommandResultValidation.None);

            var redirects = Args.Target.RedirectInputOutput;
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

        private void SerializeRewritingConfiguration()
        {
            try
            {
                using var fileStream = File.Open(RewritingConfigurationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                JsonSerializer.Serialize(fileStream, _plugin.MethodDescriptors, _jsonSerializerOptions);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Could not serialize method descriptors.", e);
            }
        }

        private RunCommandArgs LoadCommandArguments(string configurationFilePath)
        {
            try
            {
                var configurationText = File.ReadAllText(configurationFilePath);
                return JsonSerializer.Deserialize<RunCommandArgs>(configurationText, _jsonDeserializerOptions) 
                    ?? throw new JsonException($"Could not parse file: \"{configurationFilePath}\".");
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error during loading configuration.", e);
            }
        }

        private Type LoadPluginInfo()
        {
            var assemblyPath = Args.Analysis.Path;
            var pluginType = Args.Analysis.FullTypeName;

            try
            {
#pragma warning disable S3885 // "Assembly.Load" should be used because we are loading assemblies using paths
                var assembly = Assembly.LoadFile(Path.GetFullPath(assemblyPath));
#pragma warning restore S3885
                return assembly.ManifestModule.GetType(pluginType, ignoreCase: false, throwOnError: true)
                    ?? throw new TypeLoadException($"Could not find type: \"{pluginType}\" in assembly \"{assembly.FullName}\".");
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error during loading plugin.", e);
            }
        }

        private void ThrowOnInvalidConfiguration()
        {
            ThrowOnInvalidTargetConfiguration(Args.Target);
            ThrowOnInvalidRuntimeConfiguration(Args.Runtime);
            ThrowOnInvalidAnalysisConfiguration(Args.Analysis);
        }

        private static void ThrowOnInvalidTargetConfiguration(TargetConfigurationArgs configArgs)
        {
            var target = configArgs.Path;
            var architecture = configArgs.Architecture;

            if (!File.Exists(target))
                throw new ArgumentException($"Could not find target assembly: \"{target}\".");
            if (architecture != Architecture.X64)
                throw new ArgumentException($"Unsupported architecture: \"{architecture}\".");
        }

        private static void ThrowOnInvalidRuntimeConfiguration(RuntimeConfigurationArgs configArgs)
        {
            var profilerClsid = configArgs.Profiler.Clsid;
            var profilerPath = configArgs.Profiler.Path;
            var profilerEventMask = configArgs.Profiler.EventMask;

            if (!Guid.TryParse(profilerClsid, out var parsedClsid) || parsedClsid == Guid.Empty)
                throw new ArgumentException($"Invalid profiler CLSID: \"{profilerClsid}\".");
            if (string.IsNullOrWhiteSpace(profilerPath))
                throw new ArgumentException($"Invalid profiler path: \"{profilerPath}\".");
            if (profilerEventMask?.Length == 0)
                throw new ArgumentException($"Invalid profiler event mask: <empty>.");
            if (!File.Exists(profilerPath))
                throw new ArgumentException($"Could not find profiler library: \"{profilerPath}\".");

            if (configArgs.Host is { } host)
            {
                var hostPath = host.Path;

                if (string.IsNullOrWhiteSpace(hostPath))
                    throw new ArgumentException($"Invalid host path: \"{hostPath}\".");
                if (!File.Exists(hostPath))
                    throw new ArgumentException($"Could not find host executable: \"{hostPath}\".");
            }
        }

        private static void ThrowOnInvalidAnalysisConfiguration(AnalysisPluginConfigurationArgs configArgs)
        {
            var pluginTypeName = configArgs.FullTypeName;
            var pluginPath = configArgs.Path;

            if (string.IsNullOrWhiteSpace(pluginPath))
                throw new ArgumentException($"Invalid plugin path: \"{pluginPath}\".");
            if (!File.Exists(pluginPath))
                throw new ArgumentException($"Could not find plugin assembly: \"{pluginPath}\".");
            if (string.IsNullOrWhiteSpace(pluginTypeName))
                throw new ArgumentException($"Invalid plugin type fullname: \"{pluginTypeName}\".");
        }
    }
}
