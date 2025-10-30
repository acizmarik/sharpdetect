// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Commands;

namespace SharpDetect.Core.Communication;

public interface IProfilerCommandSender
{
    void SendCommand(IProfilerCommandArgs commandArgs);
    bool TrySendCommand(IProfilerCommandArgs commandArgs);
    bool TrySendCommand(IProfilerCommandArgs commandArgs, TimeSpan timeout);
}