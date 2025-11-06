// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SharpDetect.Core.Reporting.Model;

public record StackTrace(ThreadInfo ThreadInfo, ImmutableArray<StackFrame> Frames);
