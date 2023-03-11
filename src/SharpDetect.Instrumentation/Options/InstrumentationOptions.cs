// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Instrumentation;

namespace SharpDetect.Instrumentation.Options
{
    internal record struct InstrumentationOptions(RewritingOptions RewritingOptions, EntryExitHookOptions HookOptions);
}
