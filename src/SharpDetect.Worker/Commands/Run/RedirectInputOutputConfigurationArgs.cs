// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker.Commands.Run;

public record RedirectInputOutputConfigurationArgs(
    bool SingleConsoleMode,
    string? StdinFilePath,
    string? StdoutFilePath,
    string? StderrFilePath);