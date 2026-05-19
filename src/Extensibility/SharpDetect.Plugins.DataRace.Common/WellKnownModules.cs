// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SharpDetect.Plugins.DataRace.Common;

public static class WellKnownModules
{
    public static readonly ImmutableArray<string> SystemModulePrefixes = [ "System.", "Microsoft." ];
    public static readonly ImmutableArray<string> SystemAndTestFrameworksModulePrefixes =
    [ 
        "System.",
        "Microsoft.",
        "xunit.",
        "nunit.",
        "TUnit."
    ];
}