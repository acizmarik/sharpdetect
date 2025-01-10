// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Extensibility.Descriptors;

[Flags]
public enum CapturedValue : byte
{
    None = 0,
    CaptureAsValue = 1,
    CaptureAsReference = 2,
    IndirectLoad = 4
}
