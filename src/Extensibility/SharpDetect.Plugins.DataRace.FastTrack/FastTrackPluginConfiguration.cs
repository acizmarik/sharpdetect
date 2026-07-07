// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Core.Configuration;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

public sealed class FastTrackPluginConfiguration : PluginOptionsConfiguration, IPluginOptionsConfig<FastTrackPluginConfiguration>, IDataRacePluginConfiguration
{
    public static FastTrackPluginConfiguration Default => new();
    
    // Instrumentation
    public bool EnableFieldsAccessInstrumentation { get; init; } = true;
    public ImmutableArray<string> SkipInstrumentationForAssemblies { get; init; } = WellKnownModules.SystemModulePrefixes;
    
    // Stack trace collection
    public bool EnableStackTraceCollection { get; init; } = false;
    public int StackTraceCollectionMaxDepth { get; init; } = DataRaceStackTraceOptions.DefaultMaxDepth;
    public ImmutableArray<string> StackTraceCollectionForFields { get; init; } = [];
}


