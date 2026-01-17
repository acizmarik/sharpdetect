// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.DataRace.Eraser;

[Flags]
public enum FieldFlags
{
    None = 0,
    IsReadOnly = 1 << 1,
    IsThreadStatic = 1 << 2
}