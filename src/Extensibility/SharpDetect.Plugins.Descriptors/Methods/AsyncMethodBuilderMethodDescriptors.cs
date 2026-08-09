// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class AsyncMethodBuilderMethodDescriptors
{
    private const string AsyncTaskMethodBuilderTypeName = "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1";
    private const string AsyncStateMachineBoxTypeName = AsyncTaskMethodBuilderTypeName + "+AsyncStateMachineBox`1";
    private const string AsyncStateMachineBoxInterfaceName = "System.Runtime.CompilerServices.IAsyncStateMachineBox";
    private const string TaskTResultTypeName = "System.Threading.Tasks.Task`1";
    private const string ThreadTypeName = "System.Threading.Thread";

    private static readonly CapturedArgumentDescriptor BoxThisArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor CompletedTaskArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedValueDescriptor ReturnedBox =
        new((byte)nint.Size, CapturedValue.CaptureAsReference);

    private static readonly MethodDescriptor GetStateMachineBox;
    private static readonly MethodDescriptor AsyncStateMachineBoxMoveNext;
    private static readonly MethodDescriptor SetExistingTaskResult;

    static AsyncMethodBuilderMethodDescriptors()
    {
        GetStateMachineBox = new MethodDescriptor(
            MethodName: "GetStateMachineBox",
            DeclaringTypeFullName: AsyncTaskMethodBuilderTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT |
                                   CorCallingConvention.IMAGE_CEE_CS_CALLCONV_GENERIC,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateClass(AsyncStateMachineBoxInterfaceName),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateGenericMethodTypeParam(0)),
                    ArgumentTypeDescriptor.CreateByRef(
                        ArgumentTypeDescriptor.CreateGenericInst(
                            TaskTResultTypeName,
                            ArgumentTypeDescriptor.CreateGenericTypeParam(0)))
                ],
                GenericParametersCount: 1),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [],
                ReturnValue: ReturnedBox,
                MethodEnterInterpretation: null,
                MethodExitInterpretation: (ushort)RecordedEventType.AsyncStateMachineSuspend));
        
        AsyncStateMachineBoxMoveNext = new MethodDescriptor(
            MethodName: "MoveNext",
            DeclaringTypeFullName: AsyncStateMachineBoxTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateClass(ThreadTypeName)]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [BoxThisArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.AsyncStateMachineResume,
                MethodExitInterpretation: (ushort)RecordedEventType.AsyncStateMachineSegmentComplete));

        SetExistingTaskResult = new MethodDescriptor(
            MethodName: "SetExistingTaskResult",
            DeclaringTypeFullName: AsyncTaskMethodBuilderTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateGenericInst(
                        TaskTResultTypeName,
                        ArgumentTypeDescriptor.CreateGenericTypeParam(0)),
                    ArgumentTypeDescriptor.CreateGenericTypeParam(0)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [CompletedTaskArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.AsyncStateMachineComplete,
                MethodExitInterpretation: null,
                EmitExitEvent: false));
    }

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return GetStateMachineBox;
        yield return AsyncStateMachineBoxMoveNext;
        yield return SetExistingTaskResult;
    }
}
