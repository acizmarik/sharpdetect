// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue;

public enum DequeueErrorType
{
    OK = 0,
    NothingToRead = 2,
    TimeoutExceeded = 3,
    InternalError = 4,
    Unavailable = 5
}
