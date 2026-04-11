// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker.Commands.Run;

public sealed record RunCommandArgs(
    RuntimeConfigurationArgs? Runtime,
    TargetConfigurationArgs Target,
    AnalysisPluginConfigurationArgs Analysis)
{
    public RuntimeConfigurationArgs Runtime { get; init; } = Runtime ?? RuntimeConfigurationArgs.Default;
}









