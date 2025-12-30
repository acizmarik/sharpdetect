// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue;

public enum EnqueueErrorType
{
    OK = 0,
    Unavailable = 1,
    UnableToAcquireWriteLock = 2,
    NotEnoughFreeMemory = 3,
    TimeoutExceeded = 4
}
