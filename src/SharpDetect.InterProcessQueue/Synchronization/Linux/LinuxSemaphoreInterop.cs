// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpDetect.InterProcessQueue.Synchronization.Linux;

internal static partial class LinuxSemaphoreInterop
{
    private const string Library = "libc";
    
    public static IntPtr CreateOrOpen(string name, uint initialCount)
    {
        const int openOrCreateFlag = 0x40;
        const uint mode = 0x1B6;
        var handle = SemaphoreOpen(name, openOrCreateFlag, mode, initialCount);
        // Note: sem_open does not return 0 but rather -1 on failure
        if (handle != new IntPtr(-1))
            return handle;
        
        ThrowInteropFailed();
        throw new UnreachableException();
    }

    public static bool TryWait(IntPtr handle)
    {
        const int EAGAIN = 11;
        var result = SemaphoreTryWait(handle);
        if (result == 0)
            return true;
        
        var error = Marshal.GetLastPInvokeError();
        if (error == EAGAIN)
            return false;
        
        ThrowInteropFailed();
        throw new UnreachableException();
    }
    
    public static bool Wait(IntPtr handle, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var absTime = new Timespec
        {
            tv_sec = deadline.ToUnixTimeSeconds(),
            tv_nsec = (deadline.Ticks % TimeSpan.TicksPerSecond) * 100
        };

        const int EINTR = 4;
        const int ETIMEDOUT = 110;
        int error;
        
        do
        {
            var result = SemaphoreTimedWait(handle, ref absTime);
            if (result == 0)
                return true;

            error = Marshal.GetLastPInvokeError();
        } while (error == EINTR);

        if (error == ETIMEDOUT)
            return false;

        ThrowInteropFailed();
        throw new UnreachableException();
    }

    public static void Release(IntPtr handle)
    {
        if (SemaphorePost(handle) == 0)
            return;

        ThrowInteropFailed();
    }

    public static void Close(IntPtr handle)
    {
        if (SemaphoreClose(handle) == 0)
            return;
        
        ThrowInteropFailed();
    }

    public static void Unlink(string name)
    {
        if (SemaphoreUnlink(name) == 0)
            return;
        
        ThrowInteropFailed();
    }
    
    [DoesNotReturn]
    private static void ThrowInteropFailed([CallerMemberName] string caller = "")
    {
        var error = Marshal.GetLastPInvokeError();
        throw new Exception($"Operation {caller} failed. Error = {error}");
    }
    
    [LibraryImport(Library, EntryPoint = "sem_open", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint SemaphoreOpen(string name, int flags, uint mode, uint value);

    [LibraryImport(Library, EntryPoint = "sem_post", SetLastError = true)]
    private static partial int SemaphorePost(nint semaphore);

    [LibraryImport(Library, EntryPoint = "sem_trywait", SetLastError = true)]
    private static partial int SemaphoreTryWait(nint semaphore);
    
    [LibraryImport(Library, EntryPoint = "sem_timedwait", SetLastError = true)]
    private static partial int SemaphoreTimedWait(nint semaphore, ref Timespec absTime);

    [LibraryImport(Library, EntryPoint = "sem_close", SetLastError = true)]
    private static partial int SemaphoreClose(nint semaphore);

    [LibraryImport(Library, EntryPoint = "sem_unlink", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int SemaphoreUnlink(string name);
    
    [StructLayout(LayoutKind.Sequential)]
    private struct Timespec
    {
        public long tv_sec;
        public long tv_nsec;
    }
}