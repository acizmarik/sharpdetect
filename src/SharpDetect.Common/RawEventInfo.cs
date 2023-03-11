// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common
{
    public record struct RawEventInfo(ulong Id, int ProcessId, UIntPtr ThreadId);
}
