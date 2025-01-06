// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.InterProcessQueue.Configuration;

public sealed record ConsumerMemoryMappedQueueOptions : MemoryMappedQueueOptions
{
    public ConsumerMemoryMappedQueueOptions(string name, string? file, long capacity)
        : base(name, file, capacity)
    {

    }
}
