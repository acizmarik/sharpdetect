// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.InterProcessQueue.Synchronization.Linux;
using SharpDetect.InterProcessQueue.Synchronization.Windows;

namespace SharpDetect.InterProcessQueue.Synchronization;

public static class InterProcessSemaphore
{
    public static ISemaphore CreateOrOpen(string name, bool isOwner)
    {
        return OperatingSystem.IsLinux()
            ? LinuxSemaphore.CreateOrOpen(name, isOwner)
                : OperatingSystem.IsWindows() 
                ? WindowsSemaphore.CreateOrOpen(name)
                    : throw new PlatformNotSupportedException();
    }
}