// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using System.Collections.Immutable;

namespace SharpDetect.PluginHost.Services;

internal class PluginHost : IPluginHost, IDisposable
{
    private readonly ImmutableDictionary<RecordedEventHandlerType, BoundMethodEnterExitHandler> _customMethodEntryExitHandlers;
    private readonly IRecordedEventsDeliveryContext _recordedEventsDeliveryContext;
    private readonly IPlugin _plugin;
    private readonly ILogger<PluginHost> _logger;
    private bool _isDisposed;

    public PluginHost(
        IRecordedEventBindingsCompiler recordedEventBindingsCompiler,
        IRecordedEventsDeliveryContext recordedEventsDeliveryContext,
        IPlugin plugin,
        ILogger<PluginHost> logger)
    {
        _customMethodEntryExitHandlers = recordedEventBindingsCompiler.CompileCustomEventBindings(plugin);
        _recordedEventsDeliveryContext = recordedEventsDeliveryContext;
        _plugin = plugin;
        _logger = logger;
    }

    public RecordedEventState ProcessEvent(RecordedEvent recordedEvent)
    {
        if (!IsEventFromManagedThread(recordedEvent))
            return RecordedEventState.Discarded;

        BoundMethodEnterExitHandler? customHandler = null;
        if (recordedEvent.EventArgs is ICustomizableEventType customizableEventType)
        {
            var handlerType = new RecordedEventHandlerType((RecordedEventType)customizableEventType.Interpretation, recordedEvent.EventArgs.GetType());
            _customMethodEntryExitHandlers.TryGetValue(handlerType, out customHandler);
        }

        try
        {
            var processThreadId = new ProcessThreadId(recordedEvent.Metadata.Pid, recordedEvent.Metadata.Tid);
            if (_recordedEventsDeliveryContext.IsBlockedEventsDeliveryForThread(processThreadId))
            {
                // Do not execute if thread is blocked
                _recordedEventsDeliveryContext.EnqueueBlockedEventForThread(processThreadId, recordedEvent);
                return RecordedEventState.Defered;
            }

            customHandler?.Invoke(_plugin, recordedEvent.Metadata, recordedEvent.EventArgs);
            if (_recordedEventsDeliveryContext.IsBlockedEventsDeliveryForThread(processThreadId))
            {
                // Plugin just requested thread blocking
                // We will attempt to replay this event once the thread gets unblocked
                _recordedEventsDeliveryContext.EnqueueBlockedEventForThread(processThreadId, recordedEvent);
                return RecordedEventState.Defered;
            }

            var visitor = _plugin.EventsVisitor;
            visitor.Visit(recordedEvent.Metadata, recordedEvent.EventArgs);
            return RecordedEventState.Executed;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PID={Pid}][Plugin={Plugin}] An unhandled exception was thrown during a callback for event \"{Event}\".",
                recordedEvent.Metadata.Pid, _plugin.GetType().Name, recordedEvent.EventArgs.GetType().Name);
            return RecordedEventState.Failed;
        }
    }

    private static bool IsEventFromManagedThread(RecordedEvent recordedEvent)
    {
        return recordedEvent.Metadata.Tid != default;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        if (_plugin is IDisposable disposable)
            disposable.Dispose();
        GC.SuppressFinalize(this);
    }
}
