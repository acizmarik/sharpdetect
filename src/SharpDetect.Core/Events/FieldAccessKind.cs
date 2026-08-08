// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Events;

public enum FieldAccessKind : byte
{
    Regular = 0,
    Volatile = 1,
    Atomic = 2
}
