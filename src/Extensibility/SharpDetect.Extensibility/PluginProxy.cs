// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using Microsoft.Extensions.Logging;
using SharpDetect.Events;
using SharpDetect.Extensibility.Abstractions;
using SharpDetect.Reporting.Model;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace SharpDetect.Extensibility;

public class PluginProxy : IDisposable
{
    private record struct CustomHandlerType(RecordedEventType EventType, Type ArgsType);
    private readonly ImmutableDictionary<CustomHandlerType, CustomMethodEnterExit> _customMethodEntryExitHandlers;
    private readonly IEventsDeliveryContext _eventsDeliveryContext;
    private readonly ILogger _logger;
    private readonly IPlugin _plugin;
    private bool _isDisposed;

    public PluginProxy(IPlugin plugin, IEventsDeliveryContext eventsDeliveryContext, ILogger<PluginProxy> logger)
    {
        _plugin = plugin;
        _eventsDeliveryContext = eventsDeliveryContext;
        _logger = logger;
        _customMethodEntryExitHandlers = CompileCustomEventBindings();
    }

    public Summary GetReport()
        => _plugin.CreateDiagnostics();

    public EventState RelayEvent(RecordedEvent recordedEvent)
    {
        if (!IsEventFromManagedThread(recordedEvent))
            return EventState.Discarded;

        CustomMethodEnterExit? customHandler = null;
        if (recordedEvent.EventArgs is ICustomizableEventType customizableEventType)
        {
            var handlerType = new CustomHandlerType((RecordedEventType)customizableEventType.Interpretation, recordedEvent.EventArgs.GetType());
            _customMethodEntryExitHandlers.TryGetValue(handlerType, out customHandler);
        }

        try
        {
            var threadId = recordedEvent.Metadata.Tid;
            if (_eventsDeliveryContext.IsBlockedEventsDeliveryForThread(threadId))
            {
                // Do not execute if thread is blocked
                _eventsDeliveryContext.EnqueueBlockedEventForThread(threadId, recordedEvent);
                return EventState.Defered;
            }

            customHandler?.Invoke(_plugin, recordedEvent.Metadata, recordedEvent.EventArgs);
            if (_eventsDeliveryContext.IsBlockedEventsDeliveryForThread(threadId))
            {
                // Plugin just requested thread blocking
                // We will attempt to replay this event once the thread gets unblocked
                _eventsDeliveryContext.EnqueueBlockedEventForThread(threadId, recordedEvent);
                return EventState.Defered;
            }

            var visitor = _plugin.EventsVisitor;
            visitor.Visit(recordedEvent.Metadata, recordedEvent.EventArgs);
            return EventState.Executed;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PID={Pid}][Plugin={Plugin}] An unhandled exception was thrown during a callback for event \"{Event}\".",
                recordedEvent.Metadata.Pid, _plugin.GetType().Name, recordedEvent.GetType().Name);
            return EventState.Failed;
        }
    }

    private static bool IsEventFromManagedThread(RecordedEvent recordedEvent)
    {
        return recordedEvent.Metadata.Tid != default;
    }

    private ImmutableDictionary<CustomHandlerType, CustomMethodEnterExit> CompileCustomEventBindings()
    {
        var builder = ImmutableDictionary.CreateBuilder<CustomHandlerType, CustomMethodEnterExit>();
        foreach (var method in _plugin.GetType().GetMethods())
        {
            var customAttributes = method.GetCustomAttributesData();
            var eventBindAttribute = customAttributes.FirstOrDefault(a => a.AttributeType == typeof(RecordedEventBindAttribute));
            if (eventBindAttribute == null)
                continue;

            // Get custom event type
            var recordedEventType = (RecordedEventType)(ushort)eventBindAttribute.ConstructorArguments[0]!.Value!;
            var argType = method.GetParameters()[1].ParameterType;

            // Dynamically generate and compile invoker
            var parameterPlugin = Expression.Parameter(typeof(IPlugin), "plugin");
            var parameterMetadata = Expression.Parameter(typeof(RecordedEventMetadata), "metadata");
            var parameterArgs = Expression.Parameter(typeof(IRecordedEventArgs), "args");
            var upcastedPlugin = Expression.Convert(parameterPlugin, _plugin.GetType());
            var upcastedArgs = Expression.Convert(parameterArgs, argType);
            var invocation = Expression.Call(upcastedPlugin, method, parameterMetadata, upcastedArgs);
            var lambda = Expression.Lambda<Action<IPlugin, RecordedEventMetadata, IRecordedEventArgs>>
                (invocation, parameterPlugin, parameterMetadata, parameterArgs);
            CustomMethodEnterExit compiled = lambda.Compile(preferInterpretation: false).Invoke;
            builder.Add(new(recordedEventType, argType), compiled);
        }

        return builder.ToImmutable();
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
