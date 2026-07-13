// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue;

public sealed class QueueHeaderValidationException : InvalidOperationException
{
    public QueueHeaderValidationException(string message)
        : base(message)
    {
    }

    public QueueHeaderValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
