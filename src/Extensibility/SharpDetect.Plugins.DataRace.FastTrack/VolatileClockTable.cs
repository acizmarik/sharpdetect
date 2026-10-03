// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class VolatileClockTable(ThreadClockTable threadClocks) : ITrackedObjectState
{
    private readonly Dictionary<FieldId, VectorClock> _staticClocks = [];
    private readonly Dictionary<ProcessTrackedObjectId, Dictionary<FieldId, VectorClock>> _instanceClocks = [];

    public void Acquire(FieldId fieldId, ProcessTrackedObjectId? objectId, ProcessThreadId threadId)
    {
        var clocks = GetClockMap(objectId);
        if (!clocks.TryGetValue(fieldId, out var volatileVc))
        {
            volatileVc = threadClocks.CreateClock();
            clocks[fieldId] = volatileVc;
        }

        threadClocks.GetOrCreate(threadId).Join(volatileVc);
    }

    // A volatile write publishes exactly what the writing thread has done, replacing any earlier writer's clock
    public void Release(FieldId fieldId, ProcessTrackedObjectId? objectId, ProcessThreadId threadId)
    {
        var threadVc = threadClocks.GetOrCreate(threadId);
        GetClockMap(objectId)[fieldId] = threadVc.Clone();
        threadVc.Increment(threadId);
    }

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId) => _instanceClocks.Remove(objectId);

    private Dictionary<FieldId, VectorClock> GetClockMap(ProcessTrackedObjectId? objectId)
    {
        if (objectId is not { } objId)
            return _staticClocks;

        if (!_instanceClocks.TryGetValue(objId, out var clocks))
        {
            clocks = [];
            _instanceClocks[objId] = clocks;
        }

        return clocks;
    }
}
