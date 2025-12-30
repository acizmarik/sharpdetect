// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Communication;

public interface IProfilerCommandSenderProvider
{
    IProfilerCommandSender Create(string ipcQueueName, string? ipcQueueFileName, uint size);
}