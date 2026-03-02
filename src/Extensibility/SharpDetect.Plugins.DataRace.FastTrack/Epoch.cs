// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal readonly record struct Epoch(ProcessThreadId ThreadId, int Clock)
{
    public static readonly Epoch None = new(default, 0);
    public bool IsNone => Clock == 0 && ThreadId == default;
    
    public bool HappensBefore(VectorClock vc)
    {
        return vc.GetClock(ThreadId) >= Clock;
    }

    public override string ToString() => IsNone ? "⊥" : $"{ThreadId.ThreadId.Value}@{Clock}";
}

