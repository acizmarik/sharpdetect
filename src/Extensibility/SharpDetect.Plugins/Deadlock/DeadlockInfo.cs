// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Deadlock;

public record DeadlockThreadInfo(ThreadId ThreadId, string ThreadName, ThreadId BlockedOn, TrackedObjectId LockId);
public readonly record struct DeadlockInfo(uint ProcessId, List<DeadlockThreadInfo> Cycle);

