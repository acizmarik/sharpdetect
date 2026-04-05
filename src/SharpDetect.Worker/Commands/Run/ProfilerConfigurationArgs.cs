// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker.Commands.Run;

public record ProfilerConfigurationArgs(
    ProfilerPathArgs Path,
    string Clsid = ProfilerConfigurationArgs.ConcurrencyProfilerClsid,
    ProfilerLogLevel LogLevel = ProfilerLogLevel.Warning)
{
    public const string ConcurrencyProfilerClsid = "{b2c60596-b36d-460b-902a-3d91f5878529}";
}