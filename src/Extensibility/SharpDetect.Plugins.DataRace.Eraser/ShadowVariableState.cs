// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.DataRace.Eraser;

internal enum ShadowVariableState
{
    Unknown = 0,
    Virgin,
    Exclusive,
    Shared,
    SharedModified
}