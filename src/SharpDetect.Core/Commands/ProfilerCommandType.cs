// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Commands;

public enum ProfilerCommandType
{
    Unspecified = 0,
    
    CreateStackSnapshot = 1,
    CreateStackSnapshots = 2
}