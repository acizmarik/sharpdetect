// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SharpDetect.Plugins.DataRace.Common;

public interface IDataRacePluginConfiguration
{
    // Instrumentation
    bool EnableFieldsAccessInstrumentation { get; }
    ImmutableArray<string> SkipInstrumentationForAssemblies { get; }
    
    // Stack trace collection
    bool EnableStackTraceCollection { get; }
    int StackTraceCollectionMaxDepth { get; }
    ImmutableArray<string> StackTraceCollectionForFields { get; }
}

