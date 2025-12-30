// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue;

public enum DequeueErrorType
{
    OK = 0,
    UnableToAcquireReadLock = 1,
    NothingToRead = 2,
    TimeoutExceeded = 3
}
