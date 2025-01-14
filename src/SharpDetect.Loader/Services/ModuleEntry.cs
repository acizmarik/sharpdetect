// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Loader.Services;

internal readonly record struct ModuleEntry(uint ProcessId, ModuleId ModuleId);
