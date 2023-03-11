// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Services.Metadata
{
    public interface IMetadataContext
    {
        IMetadataEmitter GetEmitter(int processId);
        IMetadataResolver GetResolver(int processId);
    }
}
