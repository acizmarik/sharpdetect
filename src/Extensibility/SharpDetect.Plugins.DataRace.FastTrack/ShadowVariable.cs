// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class ShadowVariable
{
    public Epoch WriteEpoch { get; private set; }
    public Epoch ReadEpoch { get; private set; }
    public VectorClock? ReadVectorClock { get; private set; }
    public WriteKind LastWriteKind { get; private set; }
    public AtomicAccessClocks? AtomicAccesses { get; private set; }
    public InFlightAtomicAccesses? InFlightAtomicAccesses { get; private set; }
    public bool HasReadVectorClock => ReadVectorClock != null;

    private ShadowVariable()
    {
        WriteEpoch = Epoch.None;
        ReadEpoch = Epoch.None;
        ReadVectorClock = null;
        LastWriteKind = WriteKind.Regular;
    }

    public static ShadowVariable CreateVirgin() => new();

    public void SetWrite(Epoch epoch, WriteKind kind)
    {
        WriteEpoch = epoch;
        LastWriteKind = kind;
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

    public AtomicAccessClocks GetOrCreateAtomicAccesses() => AtomicAccesses ??= new AtomicAccessClocks();

    public InFlightAtomicAccesses GetOrCreateInFlightAtomicAccesses() => InFlightAtomicAccesses ??= new InFlightAtomicAccesses();

    public void ClearAtomicAccesses()
    {
        AtomicAccesses = null;
    }
}

