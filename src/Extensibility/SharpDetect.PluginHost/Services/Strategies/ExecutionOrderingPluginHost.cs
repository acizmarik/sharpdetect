// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies;

internal sealed class ExecutionOrderingPluginHost : PluginHostBase
{
    private readonly IRecordedEventsDeliveryContext _recordedEventsDeliveryContext;

    public ExecutionOrderingPluginHost(
        IRecordedEventBindingsCompiler recordedEventBindingsCompiler,
        IRecordedEventsDeliveryContext recordedEventsDeliveryContext,
        IPlugin plugin,
        ILogger<ExecutionOrderingPluginHost> logger)
        : base(recordedEventBindingsCompiler, plugin, logger)
    {
        _recordedEventsDeliveryContext = recordedEventsDeliveryContext;
    }

    protected override RecordedEventState ProcessEventCore(RecordedEvent recordedEvent, BoundMethodEnterExitHandler? customHandler)
    {
        var processThreadId = new ProcessThreadId(recordedEvent.Metadata.Pid, recordedEvent.Metadata.Tid);
        if (_recordedEventsDeliveryContext.IsBlockedEventsDeliveryForThread(processThreadId))
        {
            // Do not execute if thread is blocked
            _recordedEventsDeliveryContext.EnqueueBlockedEventForThread(processThreadId, recordedEvent);
            return RecordedEventState.Defered;
        }

        customHandler?.Invoke(Plugin, recordedEvent.Metadata, recordedEvent.EventArgs);
        if (_recordedEventsDeliveryContext.IsBlockedEventsDeliveryForThread(processThreadId))
        {
            // Plugin just requested thread blocking
            // We will attempt to replay this event once the thread gets unblocked
            _recordedEventsDeliveryContext.EnqueueBlockedEventForThread(processThreadId, recordedEvent);
            return RecordedEventState.Defered;
        }

        Plugin.EventsVisitor.Visit(recordedEvent.Metadata, recordedEvent.EventArgs);
        return RecordedEventState.Executed;
    }
}
