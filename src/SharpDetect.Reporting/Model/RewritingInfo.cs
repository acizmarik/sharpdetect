// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.Reporting.Model
{
    public record RewritingInfo(ulong AnalyzedMethodsCount, ulong InjectedTypesCount, ulong InjectedMethodsCount, ulong RewrittenMethodsCount);
}
