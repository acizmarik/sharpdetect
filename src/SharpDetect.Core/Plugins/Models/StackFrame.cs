// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Plugins.Models;

public readonly record struct StackFrame(ModuleId ModuleId, MdMethodDef MethodToken);
