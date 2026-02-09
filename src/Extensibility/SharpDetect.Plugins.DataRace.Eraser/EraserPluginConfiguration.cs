// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Configuration;

namespace SharpDetect.Plugins.DataRace.Eraser;

public sealed class EraserPluginConfiguration : PluginOptionsConfiguration, IPluginOptionsConfig<EraserPluginConfiguration>
{
    public bool SuppressAnalysisOfStaticDelegates { get; init; } = true;
    public string[] ExcludedFieldAccessModulePrefixes { get; init; } = [];
    public static EraserPluginConfiguration Default => new();
}
