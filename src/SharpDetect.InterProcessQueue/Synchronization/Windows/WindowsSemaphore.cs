// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue.Synchronization.Windows;

internal sealed class WindowsSemaphore : ISemaphore
{
    private IntPtr _handle;
    private volatile bool _isDisposed;

    private WindowsSemaphore(IntPtr handle)
    {
        _handle = handle;
    }
    
    public static WindowsSemaphore CreateOrOpen(string name)
    {
        var handle = WindowsSemaphoreInterop.Open(name, initialCount: 0);
        return new WindowsSemaphore(handle);
    }
    
    ~WindowsSemaphore()
    {
        if (!_isDisposed)
            Dispose();
    }

    public bool TryWait()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return WindowsSemaphoreInterop.TimedWait(_handle, timeoutMs: 0);
    }
    
    public bool Wait(TimeSpan timeout)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return WindowsSemaphoreInterop.TimedWait(_handle, (int)timeout.TotalMilliseconds);
    }

    public void Release()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        WindowsSemaphoreInterop.Release(_handle);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        WindowsSemaphoreInterop.Close(_handle);
        _handle = IntPtr.Zero;
        
        GC.SuppressFinalize(this);
    }
}