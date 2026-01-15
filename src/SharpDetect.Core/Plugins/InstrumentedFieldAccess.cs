// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Plugins;

public readonly record struct InstrumentedFieldAccess(ModuleId ModuleId, MdMethodDef MethodToken, MdToken FieldToken);