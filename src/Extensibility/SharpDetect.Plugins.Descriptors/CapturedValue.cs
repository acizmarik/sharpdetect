// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors;

[Flags]
public enum CapturedValue : byte
{
    None = 0,
    CaptureAsValue = 1,
    CaptureAsReference = 2,
    IndirectLoad = 4
}
