// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Core.Configuration;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.Eraser;

public sealed class EraserPluginConfiguration : PluginOptionsConfiguration, IPluginOptionsConfig<EraserPluginConfiguration>, IDataRacePluginConfiguration
{
    public bool EnableFieldsAccessInstrumentation { get; init; } = true;
    public ImmutableArray<string> SkipInstrumentationForAssemblies { get; init; } = WellKnownModules.SystemModulePrefixes;
    public bool EnableFieldAccessStackTraces { get; init; } = false;
    public int FieldAccessStackTracesMaxDepth { get; init; } = DataRaceStackTraceOptions.DefaultMaxDepth;
    public ImmutableArray<string> FieldAccessStackTracesFields { get; init; } = [];
    public static EraserPluginConfiguration Default => new();
}
