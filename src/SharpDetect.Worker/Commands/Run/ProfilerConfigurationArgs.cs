// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker.Commands.Run;

public record ProfilerConfigurationArgs(
    string Clsid,
    ProfilerPathArgs Path, 
    ProfilerLogLevel LogLevel = ProfilerLogLevel.Warning);