// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;

namespace SharpDetect.Common.Plugins
{
    public record struct EventInfo(IShadowCLR Runtime, IShadowThread Thread);
}
