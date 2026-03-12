// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Metadata;

public interface ISymbolResolver
{
    SequencePointInfo? ResolveSequencePoint(uint pid, ModuleId moduleId, int methodToken, uint ilOffset);
}

public record SequencePointInfo(string DocumentUrl, int StartLine);

