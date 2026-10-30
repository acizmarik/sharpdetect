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

        yield return Create(CountdownEventTypeName, "Signal", Boolean, [], returnsSuccess: false, RecordedEventType.SignalSet, RecordedEventType.SignalSetResult);
        yield return Create(CountdownEventTypeName, "Signal", Boolean, [Int32], returnsSuccess: false, RecordedEventType.SignalSet, RecordedEventType.SignalSetResult);
        yield return Create(CountdownEventTypeName, "Reset", Void, [Int32], returnsSuccess: false, RecordedEventType.SignalReset, RecordedEventType.SignalResetResult);
        yield return Create(CountdownEventTypeName, "Wait", Boolean, [Int32, CancellationToken], returnsSuccess: true, RecordedEventType.SignalWait, RecordedEventType.SignalWaitResult);

        yield return Create(BarrierTypeName, "SignalAndWait", Boolean, [Int32, CancellationToken], returnsSuccess: true, RecordedEventType.SignalSet, RecordedEventType.SignalWaitResult);
    }

    private static MethodDescriptor Create(
        string declaringTypeName,
        string methodName,
        ArgumentTypeDescriptor returnType,
        ArgumentTypeDescriptor[] parameterTypes,
        bool returnsSuccess,
        RecordedEventType enterInterpretation,
        RecordedEventType exitInterpretation)
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
                MethodExitInterpretation: (ushort)exitInterpretation));
    }
}
