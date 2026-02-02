// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class ShadowMemory
{
    private readonly Dictionary<FieldId, ShadowVariable> _staticShadowWords = [];
    private readonly Dictionary<(FieldId, ProcessTrackedObjectId), ShadowVariable> _instanceShadowWords = [];
    
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
        return shadow;
    }
    
    public void Update(FieldId fieldId, ProcessTrackedObjectId? objectId, ShadowVariable shadow)
    {
        if (objectId != null)
        {
            _instanceShadowWords[(fieldId, objectId.Value)] = shadow;
        }
        else
        {
            _staticShadowWords[fieldId] = shadow;
        }
    }
    
    public int Count => _staticShadowWords.Count + _instanceShadowWords.Count;
}
