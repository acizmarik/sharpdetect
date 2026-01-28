// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class ShadowMemory
{
    private readonly Dictionary<FieldId, ShadowVariable> _shadowWords = [];
    
    public ShadowVariable GetOrCreateVirgin(FieldId fieldId)
    {
        if (_shadowWords.TryGetValue(fieldId, out var shadow))
            return shadow;

        var virginShadow = ShadowVariable.CreateVirgin();
        _shadowWords[fieldId] = virginShadow;
        return virginShadow;
    }

    public void Update(FieldId fieldId, ShadowVariable shadow)
    {
        _shadowWords[fieldId] = shadow;
    }
    
    public int Count => _shadowWords.Count;
}
