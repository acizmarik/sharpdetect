// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Commands;

namespace SharpDetect.Core.Serialization;

public interface IProfilerCommandSerializer
{
    byte[] Serialize(ProfilerCommand command);
}
