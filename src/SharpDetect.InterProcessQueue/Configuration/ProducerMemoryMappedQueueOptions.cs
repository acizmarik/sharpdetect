// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue.Configuration;

public sealed record ProducerMemoryMappedQueueOptions : MemoryMappedQueueOptions
{
    public ProducerMemoryMappedQueueOptions(string name, string? file, long capacity)
        : base(name, file, capacity)
    {

    }
}
