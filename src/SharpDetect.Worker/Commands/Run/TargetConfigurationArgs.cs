// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.Worker.Commands.Run;

public record TargetConfigurationArgs(
    string Path,
    Architecture Architecture, 
    string? Args, 
    string? WorkingDirectory, 
    KeyValuePair<string, string>[]? AdditionalEnvironmentVariables,
    RedirectInputOutputConfigurationArgs? RedirectInputOutput);