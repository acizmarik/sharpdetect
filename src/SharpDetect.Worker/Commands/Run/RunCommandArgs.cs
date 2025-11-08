// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace SharpDetect.Worker.Commands.Run;

public record RunCommandArgs(
    RuntimeConfigurationArgs Runtime,
    TargetConfigurationArgs Target,
    AnalysisPluginConfigurationArgs Analysis);









