// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class ReaderWriterLockSlimMethodDescriptors
{
    private const string ReaderWriterLockSlimTypeName = "System.Threading.ReaderWriterLockSlim";

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return CreateTryEnter("TryEnterReadLock", Sig.Int32, RecordedEventType.SharedLockAcquire, RecordedEventType.SharedLockAcquireResult);
        yield return CreateTryEnter("TryEnterReadLock", Sig.TimeSpan, RecordedEventType.SharedLockAcquire, RecordedEventType.SharedLockAcquireResult);
        yield return CreateExit("ExitReadLock", RecordedEventType.SharedLockRelease);

        foreach (var name in (string[])["TryEnterUpgradeableReadLock", "TryEnterWriteLock"])
        {
            yield return CreateTryEnter(name, Sig.Int32, RecordedEventType.LockTryAcquire, RecordedEventType.LockAcquireResult);
            yield return CreateTryEnter(name, Sig.TimeSpan, RecordedEventType.LockTryAcquire, RecordedEventType.LockAcquireResult);
        }

        yield return CreateExit("ExitUpgradeableReadLock", RecordedEventType.LockRelease);
        yield return CreateExit("ExitWriteLock", RecordedEventType.LockRelease);
    }

    private static MethodDescriptor CreateTryEnter(
        string methodName,
        ArgumentTypeDescriptor timeoutType,
        RecordedEventType enterInterpretation,
        RecordedEventType exitInterpretation)
    {
        return MethodDescriptor.ForInstanceMethod(ReaderWriterLockSlimTypeName, methodName)
            .Parameters(timeoutType)
            .Returns(Sig.Boolean)
            .CaptureThis()
            .CaptureReturnValue(Capture.Boolean)
            .OnEnter(enterInterpretation)
            .OnExit(exitInterpretation)
            .Build();
    }

    private static MethodDescriptor CreateExit(
        string methodName,
        RecordedEventType enterInterpretation)
    {
        return MethodDescriptor.ForInstanceMethod(ReaderWriterLockSlimTypeName, methodName)
            .CaptureThis()
            .OnEnter(enterInterpretation)
            .Build();
    }
}
