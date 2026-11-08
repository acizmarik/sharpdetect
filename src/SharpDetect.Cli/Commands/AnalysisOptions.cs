// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Commands;

internal sealed record AnalysisOptions(
    string PluginNameOrTypeFullName,
    string TargetPath,
    bool IsTest = false,
    TestRunner? TestRunner = null,
    string? TestFilter = null,
    bool InstrumentSystemLibraries = false,
    bool CollectStackTraces = false,
    int? StackTraceDepth = null)
{
    public const string DefaultPlugin = "FastTrack";
}
