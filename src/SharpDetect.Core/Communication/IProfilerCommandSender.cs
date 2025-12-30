// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using SharpDetect.Core.Commands;

namespace SharpDetect.Core.Communication;

public interface IProfilerCommandSender
{
    CommandId SendCommand(IProfilerCommandArgs commandArgs);
    bool TrySendCommand(
        IProfilerCommandArgs commandArgs,
        [NotNullWhen(true)] out CommandId? commandId);
    bool TrySendCommand(
        IProfilerCommandArgs commandArgs,
        TimeSpan timeout,
        [NotNullWhen(true)] out CommandId? commandId);
}

public readonly record struct CommandId(ulong Value);