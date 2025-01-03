// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Metadata;

public interface IMetadataContext
{
    IMetadataEmitter GetEmitter(uint processId);
    IMetadataResolver GetResolver(uint processId);
}
