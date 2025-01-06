// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Extensibility.Models;

public readonly record struct StackFrame(ModuleId ModuleId, MdMethodDef MethodToken);
