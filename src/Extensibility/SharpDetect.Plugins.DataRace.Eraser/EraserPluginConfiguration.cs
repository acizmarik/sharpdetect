// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Core.Configuration;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.Eraser;

public sealed class EraserPluginConfiguration : PluginOptionsConfiguration, IPluginOptionsConfig<EraserPluginConfiguration>, IDataRacePluginConfiguration
{
    public static EraserPluginConfiguration Default => new();
    
    // Instrumentation
    public bool EnableFieldsAccessInstrumentation { get; init; } = true;
    public ImmutableArray<string> SkipInstrumentationForAssemblies { get; init; } = WellKnownModules.SystemModulePrefixes;
    
    // Stack trace collection
    public bool EnableStackTraceCollection { get; init; } = false;
    public int StackTraceCollectionMaxDepth { get; init; } = DataRaceStackTraceOptions.DefaultMaxDepth;
    public ImmutableArray<string> StackTraceCollectionForFields { get; init; } = [];
}
