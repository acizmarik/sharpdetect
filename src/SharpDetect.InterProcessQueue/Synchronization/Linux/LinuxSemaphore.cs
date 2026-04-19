// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.InterProcessQueue.Synchronization.Linux;

internal sealed class LinuxSemaphore : ISemaphore
{
    private readonly bool _isOwner;
    private readonly string _name;
    private IntPtr _handle;
    private volatile bool _isDisposed;
    
    private LinuxSemaphore(IntPtr handle, string name, bool isOwner)
    {
        _handle = handle;
        _name = name;
        _isOwner = isOwner;
    }

    public static LinuxSemaphore CreateOrOpen(string name, bool isOwner)
    {
        var handle = LinuxSemaphoreInterop.CreateOrOpen(name, initialCount: 0);
        return new LinuxSemaphore(handle, name, isOwner);
    }

    ~LinuxSemaphore()
    {
        if (!_isDisposed)
            Dispose();
    }

    public bool TryWait()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return LinuxSemaphoreInterop.TryWait(_handle);
    }
    
    public bool Wait(TimeSpan timeout)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return LinuxSemaphoreInterop.Wait(_handle, timeout);
    }

    public void Release()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        LinuxSemaphoreInterop.Release(_handle);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        LinuxSemaphoreInterop.Close(_handle);
        _handle = IntPtr.Zero;
        if (_isOwner)
            LinuxSemaphoreInterop.Unlink(_name);
        
        GC.SuppressFinalize(this);
    }
}