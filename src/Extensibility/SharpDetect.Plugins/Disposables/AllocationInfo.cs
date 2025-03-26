// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.Disposables;

public record AllocationInfo(
    MethodDef MethodDef, 
    uint Pid, 
    ThreadInfo ThreadInfo,
    Callstack Callstack);
