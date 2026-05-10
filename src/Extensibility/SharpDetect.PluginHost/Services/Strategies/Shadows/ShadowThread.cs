// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowThread
{
    public ProcessTrackedObjectId? BlockedOn { get; set; }
    public LinkedList<RecordedEvent> PendingQueue { get; } = [];
    public Stack<ProcessTrackedObjectId> SyncTargetStack { get; } = [];
    public int? SuspendedWaitCount { get; set; }
}
