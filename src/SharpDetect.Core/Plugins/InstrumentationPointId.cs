// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Plugins;

public readonly record struct InstrumentationPointId(uint ProcessId, ulong Id);