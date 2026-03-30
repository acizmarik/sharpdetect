// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class TaskMethodDescriptors
{
    private const string TaskTypeName = "System.Threading.Tasks.Task";
    private const string TaskTResultTypeName = "System.Threading.Tasks.Task`1";
    private const string ContinuationTaskFromTaskTypeName = "System.Threading.Tasks.ContinuationTaskFromTask";
    private const string ContinuationResultTaskFromTaskTypeName = "System.Threading.Tasks.ContinuationResultTaskFromTask`1";
    private const string ContinuationTaskFromResultTaskTypeName = "System.Threading.Tasks.ContinuationTaskFromResultTask`1";
    private const string ContinuationResultTaskFromResultTaskTypeName = "System.Threading.Tasks.ContinuationResultTaskFromResultTask`2";
    private const string ActionTypeName = "System.Action";
    private const string TaskAwaiterTypeName = "System.Runtime.CompilerServices.TaskAwaiter";
    private const string IAsyncStateMachineBoxTypeName = "System.Runtime.CompilerServices.IAsyncStateMachineBox";
    private const string ConfigureAwaitOptionsTypeName = "System.Threading.Tasks.ConfigureAwaitOptions";

    private static readonly CapturedArgumentDescriptor ObjectRefArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly MethodDescriptor TaskScheduleAndStart;
    private static readonly MethodDescriptor TaskInnerInvoke;
    private static readonly MethodDescriptor TaskTResultInnerInvoke;
    private static readonly MethodDescriptor ContinuationTaskFromTaskInnerInvoke;
    private static readonly MethodDescriptor ContinuationResultTaskFromTaskInnerInvoke;
    private static readonly MethodDescriptor ContinuationTaskFromResultTaskInnerInvoke;
    private static readonly MethodDescriptor ContinuationResultTaskFromResultTaskInnerInvoke;
    private static readonly MethodDescriptor TaskWait;
    private static readonly MethodDescriptor TaskResult;
    private static readonly MethodDescriptor TaskAwaiterValidateEnd;
    private static readonly MethodDescriptor TaskAwaiterOnCompleted;
    private static readonly MethodDescriptor TaskAwaiterUnsafeOnCompleted;

    static TaskMethodDescriptors()
    {
        TaskScheduleAndStart = new MethodDescriptor(
            MethodName: "ScheduleAndStart",
            DeclaringTypeFullName: TaskTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN)]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.TaskSchedule,
                MethodExitInterpretation: null));

        TaskInnerInvoke = CreateInnerInvokeMethodDescriptorForType(TaskTypeName);

        TaskTResultInnerInvoke = CreateInnerInvokeMethodDescriptorForType(TaskTResultTypeName);
        
        ContinuationTaskFromTaskInnerInvoke = CreateInnerInvokeMethodDescriptorForType(ContinuationTaskFromTaskTypeName);
        
        ContinuationResultTaskFromTaskInnerInvoke = CreateInnerInvokeMethodDescriptorForType(ContinuationResultTaskFromTaskTypeName);
        
        ContinuationTaskFromResultTaskInnerInvoke = CreateInnerInvokeMethodDescriptorForType(ContinuationTaskFromResultTaskTypeName);
        
        ContinuationResultTaskFromResultTaskInnerInvoke = CreateInnerInvokeMethodDescriptorForType(ContinuationResultTaskFromResultTaskTypeName);
        
        TaskWait = new MethodDescriptor(
            MethodName: "Wait",
            DeclaringTypeFullName: TaskTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: true,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.TaskJoinStart,
                MethodExitInterpretation: (ushort)RecordedEventType.TaskJoinFinish));
        
        TaskResult = new MethodDescriptor(
            MethodName: "get_Result",
            DeclaringTypeFullName: TaskTResultTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateGenericTypeParam(0),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.TaskJoinStart,
                MethodExitInterpretation: (ushort)RecordedEventType.TaskJoinFinish));
        
        TaskAwaiterValidateEnd = new MethodDescriptor(
            MethodName: "ValidateEnd",
            DeclaringTypeFullName: TaskAwaiterTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 2,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateClass(TaskTypeName),
                    ArgumentTypeDescriptor.CreateValueType(ConfigureAwaitOptionsTypeName)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference)) ],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.TaskJoinStart,
                MethodExitInterpretation: (ushort)RecordedEventType.TaskJoinFinish));
        
        TaskAwaiterOnCompleted= new MethodDescriptor(
            MethodName: "OnCompletedInternal",
            DeclaringTypeFullName: TaskAwaiterTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 4,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateClass(TaskTypeName),
                    ArgumentTypeDescriptor.CreateClass(ActionTypeName),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference)) ],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.TaskJoinStart,
                MethodExitInterpretation: (ushort)RecordedEventType.TaskJoinFinish));
        
        TaskAwaiterUnsafeOnCompleted= new MethodDescriptor(
            MethodName: "UnsafeOnCompletedInternal",
            DeclaringTypeFullName: TaskAwaiterTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
                ParametersCount: 3,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements:
                [
                    ArgumentTypeDescriptor.CreateClass(TaskTypeName),
                    ArgumentTypeDescriptor.CreateClass(IAsyncStateMachineBoxTypeName),
                    ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN)
                ]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference)) ],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.TaskJoinStart,
                MethodExitInterpretation: (ushort)RecordedEventType.TaskJoinFinish));
    }

    private static MethodDescriptor CreateInnerInvokeMethodDescriptorForType(string typeName)
    {
        return new MethodDescriptor(
            MethodName: "InnerInvoke",
            DeclaringTypeFullName: typeName,
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
                MethodEnterInterpretation: (ushort)RecordedEventType.TaskStart,
                MethodExitInterpretation: (ushort)RecordedEventType.TaskComplete));
    }
    
    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        // Internal API
        yield return TaskScheduleAndStart;
        yield return TaskInnerInvoke;
        yield return TaskTResultInnerInvoke;
        yield return ContinuationTaskFromTaskInnerInvoke;
        yield return ContinuationResultTaskFromTaskInnerInvoke;
        yield return ContinuationTaskFromResultTaskInnerInvoke;
        yield return ContinuationResultTaskFromResultTaskInnerInvoke;
        yield return TaskAwaiterValidateEnd;
        yield return TaskAwaiterOnCompleted;
        yield return TaskAwaiterUnsafeOnCompleted;

        // Common public API
        yield return TaskWait;
        yield return TaskResult;
    }
}

