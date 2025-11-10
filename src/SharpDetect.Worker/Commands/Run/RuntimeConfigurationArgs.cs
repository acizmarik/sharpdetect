// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker.Commands.Run;

public record RuntimeConfigurationArgs(
    HostConfigurationArgs? Host, 
    ProfilerConfigurationArgs Profiler);