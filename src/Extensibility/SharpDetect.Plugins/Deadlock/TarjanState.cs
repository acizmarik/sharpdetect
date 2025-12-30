// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.Deadlock;

internal sealed class TarjanState
{
    public int Index { get; set; }
    public Stack<ProcessThreadId> Stack { get; } = new();
    public Dictionary<ProcessThreadId, int> Indices { get; } = [];
    public Dictionary<ProcessThreadId, int> LowLinks { get; } = [];
    public HashSet<ProcessThreadId> OnStack { get; } = [];
}