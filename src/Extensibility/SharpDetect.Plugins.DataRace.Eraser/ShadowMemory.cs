// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class ShadowMemory
{
    private readonly Dictionary<FieldId, ShadowVariable> _staticShadowWords = [];
    private readonly Dictionary<(FieldId, ProcessTrackedObjectId), ShadowVariable> _instanceShadowWords = [];
    private readonly Dictionary<ProcessTrackedObjectId, HashSet<FieldId>> _objectFieldIndex = [];
    
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
        var key = (fieldId, objectId);
        if (_instanceShadowWords.TryGetValue(key, out var shadow))
            return shadow;

        shadow = ShadowVariable.CreateVirgin();
        _instanceShadowWords[key] = shadow;
        TrackObjectField(objectId, fieldId);
        return shadow;
    }
    
    public void Update(FieldId fieldId, ProcessTrackedObjectId? objectId, ShadowVariable shadow)
    {
        if (objectId != null)
        {
            _instanceShadowWords[(fieldId, objectId.Value)] = shadow;
            TrackObjectField(objectId.Value, fieldId);
        }
        else
        {
            _staticShadowWords[fieldId] = shadow;
        }
    }
    
    public void RemoveTrackedObjects(uint processId, ReadOnlySpan<TrackedObjectId> removedObjectIds)
    {
        foreach (var objectId in removedObjectIds)
        {
            var processObjectId = new ProcessTrackedObjectId(processId, objectId);
            if (!_objectFieldIndex.Remove(processObjectId, out var fieldIds))
                continue;
            
            foreach (var fieldId in fieldIds)
                _instanceShadowWords.Remove((fieldId, processObjectId));
        }
    }

    public int Count => _staticShadowWords.Count + _instanceShadowWords.Count;
    
    private void TrackObjectField(ProcessTrackedObjectId objectId, FieldId fieldId)
    {
        if (!_objectFieldIndex.TryGetValue(objectId, out var fieldIds))
        {
            fieldIds = [];
            _objectFieldIndex[objectId] = fieldIds;
        }

        fieldIds.Add(fieldId);
    }
}
