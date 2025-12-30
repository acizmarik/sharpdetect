// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue;

public sealed class QueueMemoryCorruptionException : InvalidOperationException
{
    public QueueMemoryCorruptionException()
        : base("Detected shared buffer corruption.")
    {
    }

    public QueueMemoryCorruptionException(string message)
        : base(message)
    {
    }

    public QueueMemoryCorruptionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}