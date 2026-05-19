// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SharpDetect.Plugins.DataRace.Common;

public interface IDataRacePluginConfiguration
{
    bool EnableFieldsAccessInstrumentation { get; }
    ImmutableArray<string> SkipInstrumentationForAssemblies { get; }
}

