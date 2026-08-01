// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.DataRace.Common;

public readonly record struct RaceKey(FieldId FieldId, RaceSite First, RaceSite Second)
{
    public static RaceKey Create(FieldId fieldId, RaceSite earlier, RaceSite later)
        => earlier.CompareTo(later) <= 0
            ? new RaceKey(fieldId, earlier, later)
            : new RaceKey(fieldId, later, earlier);
}
