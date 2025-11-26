// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.Deadlock;

public enum BlockedOnType
{
    Lock,
    Thread
}

public sealed record WaitInfo(
    BlockedOnType BlockedOnType,
    ProcessThreadId? BlockedOnProcessThreadId,
    ProcessTrackedObjectId? ProcessLockObjectId);

public sealed record DeadlockThreadInfo(
    ProcessThreadId ProcessThreadId, 
    string ThreadName, 
    ProcessThreadId BlockedOn, 
    BlockedOnType BlockedOnType,
    ProcessTrackedObjectId? ProcessLockObjectId = null);

public readonly record struct DeadlockInfo(uint ProcessId, List<DeadlockThreadInfo> Cycle);

