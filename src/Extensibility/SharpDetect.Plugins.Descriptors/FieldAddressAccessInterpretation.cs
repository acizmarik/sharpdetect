// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors;

public enum FieldAddressAccessInterpretation : byte
{
    AtomicReadModifyWrite = 1,
    VolatileRead = 2,
    VolatileWrite = 3
}
