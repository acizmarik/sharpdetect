// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Configuration;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

public sealed class FastTrackPluginConfiguration : PluginOptionsConfiguration, IPluginOptionsConfig<FastTrackPluginConfiguration>, IDataRacePluginConfiguration
{
    public string[] ExcludedFieldAccessModulePrefixes { get; init; } = [];
    public static FastTrackPluginConfiguration Default => new();
}


