// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace SharpDetect.Plugins.DataRace.Eraser;

[DebuggerDisplay("LockSet#{Value}")]
internal readonly record struct LockSetIndex(int Value)
{
    public static readonly LockSetIndex Empty = new(0);
    public bool IsEmpty => Value == 0;
}
