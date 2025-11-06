// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue.Configuration;

public record MemoryMappedQueueOptions(string Name, string? File, long Capacity);
