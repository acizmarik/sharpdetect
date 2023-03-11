// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Instrumentation;

namespace SharpDetect.Instrumentation.Options
{
    internal record struct RewritingPattern(string Pattern, InstrumentationTarget Target);
    internal record struct RewritingOptions(bool Enabled, InstrumentationStrategy Strategy, RewritingPattern[] Patterns);

    internal record struct EntryExitHookOptions(bool Enabled, InstrumentationStrategy Strategy, string[] Patterns);
}
