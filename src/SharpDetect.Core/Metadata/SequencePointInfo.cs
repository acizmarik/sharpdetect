// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SharpDetect.Core.Metadata;

public record SequencePointInfo(
    string DocumentUrl,
    int StartLine,
    Guid DocumentHashAlgorithm,
    ImmutableArray<byte> DocumentHash);
