// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.InterProcessQueue.Memory;

public interface ILocalMemory<T> : IMemory<T>
{
    ReadOnlyMemory<T> GetLocalMemory();
}
