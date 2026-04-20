// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.InterProcessQueue.Synchronization.Windows;

internal static partial class WindowsSemaphoreInterop
{
    private const string Lib = "kernel32";

    internal static nint Open(string name, int initialCount)
    {
        var handle = CreateSemaphore(0, initialCount, int.MaxValue, name);
        if (handle == 0)
            throw new InvalidOperationException(
                $"CreateSemaphoreW(\"{name}\") failed. Error = {Marshal.GetLastPInvokeError()}");
        return handle;
    }

    internal static void Release(nint sem)
    {
        ReleaseSemaphore(sem, 1, out _);
    }

    internal static bool TimedWait(nint sem, int timeoutMs)
    {
        return WaitForSingleObject(sem, timeoutMs) == 0;
    }

    internal static void Close(nint sem)
    {
        CloseSemaphore(sem);
    }
    
    [LibraryImport(Lib, EntryPoint = "CreateSemaphoreW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateSemaphore(nint lpSecurityAttributes, int lInitialCount, int lMaximumCount, string? lpName);

    [LibraryImport(Lib, EntryPoint = "ReleaseSemaphore", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReleaseSemaphore(nint hSemaphore, int lReleaseCount, out int lpPreviousCount);

    [LibraryImport(Lib, EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static partial uint WaitForSingleObject(nint hHandle, int dwMilliseconds);

    [LibraryImport(Lib, EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseSemaphore(nint hObject);
}