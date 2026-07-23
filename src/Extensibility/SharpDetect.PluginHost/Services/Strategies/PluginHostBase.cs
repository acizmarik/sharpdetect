// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using System.Collections.Frozen;

namespace SharpDetect.PluginHost.Services.Strategies;

internal abstract class PluginHostBase : IPluginHost, IDisposable
{
    protected FrozenDictionary<RecordedEventHandlerType, RecordedEventHandler> CustomMethodEntryExitHandlers { get; }
    protected IPlugin Plugin { get; }
    protected ILogger Logger { get; }
    private bool _isDisposed;

    protected PluginHostBase(IPlugin plugin, ILogger logger)
    {
        CustomMethodEntryExitHandlers = plugin.CustomEventHandlers.ToFrozenDictionary();
        Plugin = plugin;
        Logger = logger;
    }

    public RecordedEventState ProcessEvent(RecordedEvent recordedEvent)
    {
        if (!ShouldProcessEvent(recordedEvent))
            return RecordedEventState.Discarded;

        RecordedEventHandler? customHandler = null;
        if (recordedEvent.EventArgs is ICustomizableEventType customizableEventType)
        {
            var handlerType = new RecordedEventHandlerType((RecordedEventType)customizableEventType.Interpretation, recordedEvent.EventArgs.GetType());
            CustomMethodEntryExitHandlers.TryGetValue(handlerType, out customHandler);
        }

        try
        {
            return ProcessEventCore(recordedEvent, customHandler);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[PID={Pid}][Plugin={Plugin}] An unhandled exception was thrown during a callback for event \"{Event}\".",
                recordedEvent.Metadata.Pid, Plugin.GetType().Name, recordedEvent.EventArgs.GetType().Name);
            return RecordedEventState.Failed;
        }
    }
    
    protected virtual RecordedEventState ProcessEventCore(RecordedEvent recordedEvent, RecordedEventHandler? customHandler)
    {
        customHandler?.Invoke(recordedEvent.Metadata, recordedEvent.EventArgs);
        Plugin.EventsVisitor.Visit(recordedEvent.Metadata, recordedEvent.EventArgs);
        return RecordedEventState.Executed;
    }

    private static bool ShouldProcessEvent(RecordedEvent recordedEvent)
    {
        // Native threads don't have managed thread ID (native threads are represented with Tid = 0)
        // Profiler initialize & load are sent from native thread during runtime initialization
        // Command responses are emitted by the profiler's command dispatcher thread
        return recordedEvent.Metadata.Tid != default ||
               recordedEvent.Metadata.CommandId is not null ||
               recordedEvent.EventArgs is ProfilerInitializeRecordedEvent ||
               recordedEvent.EventArgs is ProfilerLoadRecordedEvent;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        if (Plugin is IDisposable disposable)
            disposable.Dispose();
        GC.SuppressFinalize(this);
    }
}

