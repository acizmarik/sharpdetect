// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class ShadowVariable
{
    public Epoch WriteEpoch { get; private set; }
    public Epoch ReadEpoch { get; private set; }
    public VectorClock? ReadVectorClock { get; private set; }
    public ProcessThreadId? ExclusiveWriteThread { get; private set; }
    public bool HasReadVectorClock => ReadVectorClock != null;
    private bool _hasMultipleWriters;

    private ShadowVariable()
    {
        WriteEpoch = Epoch.None;
        ReadEpoch = Epoch.None;
        ReadVectorClock = null;
        ExclusiveWriteThread = null;
    }

    public static ShadowVariable CreateVirgin() => new();

    public void SetWrite(Epoch epoch)
    {
        WriteEpoch = epoch;
        
        if (!_hasMultipleWriters)
        {
            if (ExclusiveWriteThread == null)
                ExclusiveWriteThread = epoch.ThreadId;
            else if (ExclusiveWriteThread != epoch.ThreadId)
            {
                ExclusiveWriteThread = null;
                _hasMultipleWriters = true;
            }
        }
    }

    public void SetRead(Epoch epoch)
    {
        ReadEpoch = epoch;
        ReadVectorClock = null;
    }

    public void ExpandReadToVectorClock(VectorClock vc)
    {
        ReadVectorClock = vc;
    }

    public string GetStateDescription()
    {
        var writeStr = WriteEpoch.IsNone ? "W:⊥" : $"W:{WriteEpoch}";
        string readStr;
        if (HasReadVectorClock)
            readStr = "R:VC";
        else if (ReadEpoch.IsNone)
            readStr = "R:⊥";
        else
            readStr = $"R:{ReadEpoch}";
        return $"[{writeStr}, {readStr}]";
    }

    public string GetStateDescription(Func<ProcessThreadId, string?> threadNameResolver)
    {
        var writeStr = WriteEpoch.IsNone ? "W:⊥" : $"W:{WriteEpoch.ToDisplayString(threadNameResolver)}";
        string readStr;
        if (HasReadVectorClock)
            readStr = "R:VC";
        else if (ReadEpoch.IsNone)
            readStr = "R:⊥";
        else
            readStr = $"R:{ReadEpoch.ToDisplayString(threadNameResolver)}";
        return $"[{writeStr}, {readStr}]";
    }
}

