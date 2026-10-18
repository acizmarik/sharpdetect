// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class TrackedObjectSet : ITrackedObjectState
{
    private readonly HashSet<ProcessTrackedObjectId> _objects = [];

    public void Add(ProcessTrackedObjectId objectId) => _objects.Add(objectId);

    public bool Contains(ProcessTrackedObjectId objectId) => _objects.Contains(objectId);

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId) => _objects.Remove(objectId);
}
