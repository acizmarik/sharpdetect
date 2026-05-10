// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Tests;

internal sealed class RecordingPluginHost : IPluginHost
{
    public List<RecordedEvent> Dispatched { get; } = [];

    public RecordedEventState ProcessEvent(RecordedEvent recordedEvent)
    {
        Dispatched.Add(recordedEvent);
        return RecordedEventState.Executed;
    }
}
