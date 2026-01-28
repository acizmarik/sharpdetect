// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using System.Collections.Immutable;

namespace SharpDetect.PluginHost.Services.Strategies;

internal abstract class PluginHostBase : IPluginHost, IDisposable
{
    protected ImmutableDictionary<RecordedEventHandlerType, BoundMethodEnterExitHandler> CustomMethodEntryExitHandlers { get; }
    protected IPlugin Plugin { get; }
    protected ILogger Logger { get; }
    private bool _isDisposed;

    protected PluginHostBase(
        IRecordedEventBindingsCompiler recordedEventBindingsCompiler,
        IPlugin plugin,
        ILogger logger)
    {
        CustomMethodEntryExitHandlers = recordedEventBindingsCompiler.CompileCustomEventBindings(plugin);
        Plugin = plugin;
        Logger = logger;
    }

    public RecordedEventState ProcessEvent(RecordedEvent recordedEvent)
    {
        if (!IsEventFromManagedThread(recordedEvent))
            return RecordedEventState.Discarded;

        BoundMethodEnterExitHandler? customHandler = null;
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
    
    protected virtual RecordedEventState ProcessEventCore(RecordedEvent recordedEvent, BoundMethodEnterExitHandler? customHandler)
    {
        customHandler?.Invoke(Plugin, recordedEvent.Metadata, recordedEvent.EventArgs);
        Plugin.EventsVisitor.Visit(recordedEvent.Metadata, recordedEvent.EventArgs);
        return RecordedEventState.Executed;
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
        if (Plugin is IDisposable disposable)
            disposable.Dispose();
        GC.SuppressFinalize(this);
    }
}

