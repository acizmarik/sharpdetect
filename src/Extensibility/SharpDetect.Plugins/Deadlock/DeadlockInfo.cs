// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Deadlock;

public readonly record struct DeadlockInfo(uint ProcessId, List<(ThreadId ThreadId, string ThreadName, ThreadId BlockedOn, TrackedObjectId LockId)> Cycle);

