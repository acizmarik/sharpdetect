// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class ReaderWriterLockSlimMethodDescriptors
{
    private const string ReaderWriterLockSlimTypeName = "System.Threading.ReaderWriterLockSlim";

    private static readonly CapturedArgumentDescriptor ObjectRefArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedValueDescriptor ReturnedSuccess = new(sizeof(bool), CapturedValue.CaptureAsValue);

    private static ArgumentTypeDescriptor Int32 => ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4);
    private static ArgumentTypeDescriptor TimeSpan => ArgumentTypeDescriptor.CreateValueType("System.TimeSpan");

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return CreateTryEnter("TryEnterReadLock", Int32, RecordedEventType.SharedLockAcquire, RecordedEventType.SharedLockAcquireResult);
        yield return CreateTryEnter("TryEnterReadLock", TimeSpan, RecordedEventType.SharedLockAcquire, RecordedEventType.SharedLockAcquireResult);
        yield return CreateExit("ExitReadLock", RecordedEventType.SharedLockRelease, RecordedEventType.SharedLockReleaseResult);

        foreach (var name in (string[])["TryEnterUpgradeableReadLock", "TryEnterWriteLock"])
        {
            yield return CreateTryEnter(name, Int32, RecordedEventType.LockTryAcquire, RecordedEventType.LockAcquireResult);
            yield return CreateTryEnter(name, TimeSpan, RecordedEventType.LockTryAcquire, RecordedEventType.LockAcquireResult);
        }

        yield return CreateExit("ExitUpgradeableReadLock", RecordedEventType.LockRelease, RecordedEventType.LockReleaseResult);
        yield return CreateExit("ExitWriteLock", RecordedEventType.LockRelease, RecordedEventType.LockReleaseResult);
    }

    private static MethodDescriptor CreateTryEnter(
        string methodName,
        ArgumentTypeDescriptor timeoutType,
        RecordedEventType enterInterpretation,
        RecordedEventType exitInterpretation)
    {
        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: ReaderWriterLockSlimTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: [timeoutType]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: ReturnedSuccess,
                MethodEnterInterpretation: (ushort)enterInterpretation,
                MethodExitInterpretation: (ushort)exitInterpretation));
    }

    private static MethodDescriptor CreateExit(
        string methodName,
        RecordedEventType enterInterpretation,
        RecordedEventType exitInterpretation)
    {
        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: ReaderWriterLockSlimTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)enterInterpretation,
                MethodExitInterpretation: (ushort)exitInterpretation));
    }
}
