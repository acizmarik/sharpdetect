// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Reporting.Model;

public record RewritingInfo(
    ulong AnalyzedMethodsCount,
    ulong InjectedTypesCount,
    ulong InjectedMethodsCount,
    ulong RewrittenMethodsCount);
