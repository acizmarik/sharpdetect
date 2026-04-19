// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue.Synchronization;

public interface ISemaphore : IDisposable
{
    bool TryWait();
    bool Wait(TimeSpan timeout);
    void Release();
}