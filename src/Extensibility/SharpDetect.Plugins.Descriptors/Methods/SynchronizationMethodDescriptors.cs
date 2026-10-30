// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class SynchronizationMethodDescriptors
{
    public static IEnumerable<MethodDescriptor> GetAllMethods()
        => GetLockMethods()
            .Concat(TaskMethodDescriptors.GetAllMethods())
            .Concat(AsyncMethodBuilderMethodDescriptors.GetAllMethods())
            .Concat(SemaphoreSlimMethodDescriptors.GetAllMethods())
            .Concat(WaitHandleMethodDescriptors.GetAllMethods())
            .Concat(SignalMethodDescriptors.GetAllMethods())
            .Concat(ConcurrentDictionaryMethodDescriptors.GetAllMethods())
            .Concat(LazyMethodDescriptors.GetAllMethods())
            .Concat(ConcurrentCollectionMethodDescriptors.GetAllMethods());

    public static IEnumerable<MethodDescriptor> GetLockMethods()
        => MonitorMethodDescriptors.GetAllMethods()
            .Concat(LockMethodDescriptors.GetAllMethods())
            .Concat(ReaderWriterLockSlimMethodDescriptors.GetAllMethods())
            .Concat(ThreadMethodDescriptors.GetAllMethods());
}
