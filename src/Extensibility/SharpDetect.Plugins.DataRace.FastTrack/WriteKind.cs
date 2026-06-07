// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal enum WriteKind
{
    Regular,
    Instantiation,
    MaybeInstantiation
}
