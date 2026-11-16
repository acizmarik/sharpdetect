// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class SignalMethodDescriptors
{
    private const string ManualResetEventSlimTypeName = "System.Threading.ManualResetEventSlim";
    private const string CountdownEventTypeName = "System.Threading.CountdownEvent";
    private const string BarrierTypeName = "System.Threading.Barrier";
    private const string CancellationTokenTypeName = "System.Threading.CancellationToken";
    private const string CancellationTokenSourceTypeName = "System.Threading.CancellationTokenSource";
    private const string ParallelReplicaTypeName = "System.Threading.Tasks.TaskReplicator+Replica";
    private const string ParallelReplicaTStateTypeName = "System.Threading.Tasks.TaskReplicator+Replica`1";

    private static readonly CapturedArgumentDescriptor ObjectRefArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedValueDescriptor ReturnedSuccess = new(sizeof(bool), CapturedValue.CaptureAsValue);

    private static ArgumentTypeDescriptor Int32 => ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4);
    private static ArgumentTypeDescriptor Boolean => ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN);
    private static ArgumentTypeDescriptor Void => ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID);
    private static ArgumentTypeDescriptor CancellationToken => ArgumentTypeDescriptor.CreateValueType(CancellationTokenTypeName);

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return Create(ManualResetEventSlimTypeName, "Set", Void, [Boolean], returnsSuccess: false, RecordedEventType.SignalSet, RecordedEventType.SignalSetResult);
        yield return Create(ManualResetEventSlimTypeName, "Reset", Void, [], returnsSuccess: false, RecordedEventType.SignalReset, RecordedEventType.SignalResetResult);
        yield return Create(ManualResetEventSlimTypeName, "Wait", Boolean, [Int32, CancellationToken], returnsSuccess: true, RecordedEventType.SignalWait, RecordedEventType.SignalWaitResult);

        yield return Create(CountdownEventTypeName, "Signal", Boolean, [], returnsSuccess: false, RecordedEventType.SignalSet, RecordedEventType.SignalSetResult, DeclaringAssemblies.Threading);
        yield return Create(CountdownEventTypeName, "Signal", Boolean, [Int32], returnsSuccess: false, RecordedEventType.SignalSet, RecordedEventType.SignalSetResult, DeclaringAssemblies.Threading);
        yield return Create(CountdownEventTypeName, "Reset", Void, [Int32], returnsSuccess: false, RecordedEventType.SignalReset, RecordedEventType.SignalResetResult, DeclaringAssemblies.Threading);
        yield return Create(CountdownEventTypeName, "Wait", Boolean, [Int32, CancellationToken], returnsSuccess: true, RecordedEventType.SignalWait, RecordedEventType.SignalWaitResult, DeclaringAssemblies.Threading);

        yield return Create(BarrierTypeName, "SignalAndWait", Boolean, [Int32, CancellationToken], returnsSuccess: true, RecordedEventType.SignalSet, RecordedEventType.SignalWaitResult, DeclaringAssemblies.Threading);

        yield return Create(CancellationTokenSourceTypeName, "TransitionToCancellationRequested", Boolean, [], returnsSuccess: false, RecordedEventType.SignalSet, RecordedEventType.SignalSetResult);
        yield return Create(CancellationTokenSourceTypeName, "get_IsCancellationRequested", Boolean, [], returnsSuccess: true, RecordedEventType.SignalWait, RecordedEventType.SignalWaitResult);

        // Internal API behind Parallel.For and Parallel.ForEach
        yield return Create(ParallelReplicaTStateTypeName, "ExecuteAction", Void, [ArgumentTypeDescriptor.CreateByRef(Boolean)], returnsSuccess: false, RecordedEventType.SignalSetOnReturn, RecordedEventType.SignalSetOnReturnResult, DeclaringAssemblies.ThreadingTasksParallel);
        yield return Create(ParallelReplicaTypeName, "Wait", Void, [], returnsSuccess: false, RecordedEventType.SignalWait, RecordedEventType.SignalWaitResult, DeclaringAssemblies.ThreadingTasksParallel);
    }

    private static MethodDescriptor Create(
        string declaringTypeName,
        string methodName,
        ArgumentTypeDescriptor returnType,
        ArgumentTypeDescriptor[] parameterTypes,
        bool returnsSuccess,
        RecordedEventType enterInterpretation,
        RecordedEventType exitInterpretation,
        string declaringAssemblyName = DeclaringAssemblies.CoreLib)
    {
        return new MethodDescriptor(
            MethodName: methodName,
            DeclaringTypeFullName: declaringTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: (byte)parameterTypes.Length,
                ReturnType: returnType,
                ArgumentTypeElements: parameterTypes),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: returnsSuccess ? ReturnedSuccess : null,
                MethodEnterInterpretation: (ushort)enterInterpretation,
                MethodExitInterpretation: (ushort)exitInterpretation),
            DeclaringAssemblyName: declaringAssemblyName);
    }
}
