// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Reporting.Model;

public sealed record StackFrame(
    string MethodName,
    string ModulePath,
    MdMethodDef MethodToken,
    IlLocation? Il,
    SourceLocation? Source);
