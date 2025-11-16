// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue.Tests;

public abstract class InterProcessQueueTestsBase : IDisposable
{
    protected readonly string TestQueueName;
    protected readonly string TestFileName;
    protected readonly int TestQueueSize;
    private bool _disposed;

    protected InterProcessQueueTestsBase(string queueName, string queueFile, int size)
    {
        TestQueueName = queueName;
        TestFileName = queueFile;
        TestQueueSize = size;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        if (File.Exists(TestFileName))
            File.Delete(TestFileName);
    }
}