// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Plugins;

public interface ICallstackResolver
{
    StackTrace Resolve(ThreadInfo threadInfo, Callstack callstack);
}
