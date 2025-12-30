// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SharpDetect.Core.Plugins;

public interface IRecordedEventBindingsCompiler
{
    ImmutableDictionary<RecordedEventHandlerType, BoundMethodEnterExitHandler> CompileCustomEventBindings(IPlugin plugin);
}
