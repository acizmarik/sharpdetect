// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class ShadowMemory
{
    private readonly Dictionary<FieldId, ShadowVariable> _staticShadowWords = [];
    private readonly Dictionary<ProcessTrackedObjectId, Dictionary<FieldId, ShadowVariable>> _instanceShadowWords = [];
    private int _instanceCount;

    public ShadowVariable GetOrCreateVirgin(FieldId fieldId, ProcessTrackedObjectId? objectId)
    {
        return objectId != null
            ? GetOrCreateInstance(fieldId, objectId.Value)
            : GetOrCreateStatic(fieldId);
    }

    private ShadowVariable GetOrCreateStatic(FieldId fieldId)
    {
        if (_staticShadowWords.TryGetValue(fieldId, out var shadow))
            return shadow;

        shadow = ShadowVariable.CreateVirgin();
        _staticShadowWords[fieldId] = shadow;
        return shadow;
    }

    private ShadowVariable GetOrCreateInstance(FieldId fieldId, ProcessTrackedObjectId objectId)
    {
        if (!_instanceShadowWords.TryGetValue(objectId, out var fields))
        {
            fields = [];
            _instanceShadowWords[objectId] = fields;
        }

        if (fields.TryGetValue(fieldId, out var shadow))
            return shadow;

        shadow = ShadowVariable.CreateVirgin();
        fields[fieldId] = shadow;
        _instanceCount++;
        return shadow;
    }

    public void RemoveTrackedObjects(uint processId, ReadOnlySpan<TrackedObjectId> removedObjectIds)
    {
        foreach (var objectId in removedObjectIds)
        {
            var processObjectId = new ProcessTrackedObjectId(processId, objectId);
            if (_instanceShadowWords.Remove(processObjectId, out var fields))
                _instanceCount -= fields.Count;
        }
    }

    public int Count => _staticShadowWords.Count + _instanceCount;
}
