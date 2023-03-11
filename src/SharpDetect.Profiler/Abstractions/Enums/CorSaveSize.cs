// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

[Flags]
public enum CorSaveSize
{
    cssAccurate = 0x0000,
    cssQuick = 0x0001,
    cssDiscardTransientCAs = 0x0002
}
