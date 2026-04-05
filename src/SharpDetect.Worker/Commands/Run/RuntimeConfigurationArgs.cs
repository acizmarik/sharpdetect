// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker.Commands.Run;

public record RuntimeConfigurationArgs(
    HostConfigurationArgs? Host, 
    ProfilerConfigurationArgs? Profiler)
{
    public ProfilerConfigurationArgs Profiler { get; init; } = Profiler ?? new ProfilerConfigurationArgs();
    public static readonly RuntimeConfigurationArgs Default = new(
        Host: null,
        Profiler: new ProfilerConfigurationArgs());
}
