// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Core.Configuration;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

public sealed class FastTrackPluginConfiguration : PluginOptionsConfiguration, IPluginOptionsConfig<FastTrackPluginConfiguration>, IDataRacePluginConfiguration
{
    public bool EnableFieldsAccessInstrumentation { get; init; } = true;
    public ImmutableArray<string> SkipInstrumentationForAssemblies { get; init; } = WellKnownModules.SystemModulePrefixes;
    public static FastTrackPluginConfiguration Default => new();
}


