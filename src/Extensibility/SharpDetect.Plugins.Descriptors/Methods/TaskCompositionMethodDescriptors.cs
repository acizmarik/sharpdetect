// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors.Methods;

public static class TaskCompositionMethodDescriptors
{
    private const string TaskTypeName = "System.Threading.Tasks.Task";
    private const string WhenAllPromiseTypeName = TaskTypeName + "+WhenAllPromise";
    private const string WhenAllPromiseTResultTypeName = TaskTypeName + "+WhenAllPromise`1";
    private const string CountdownPromiseTypeName = "System.Threading.Tasks.TaskFactory+CompleteOnCountdownPromise";
    private const string CountdownPromiseTResultTypeName = "System.Threading.Tasks.TaskFactory+CompleteOnCountdownPromise`1";

    private static readonly CapturedArgumentDescriptor ObjectRefArg =
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    private static readonly CapturedArgumentDescriptor CompletedTaskArg =
        new(1, new((byte)nint.Size, CapturedValue.CaptureAsReference));

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        yield return new MethodDescriptor(
            MethodName: "get_IsWaitNotificationEnabled",
            DeclaringTypeFullName: TaskTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 0,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN),
                ArgumentTypeElements: []),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.TaskCompletionObserved,
                MethodExitInterpretation: null,
                EmitExitEvent: false));

        // Internal API: invoked once per awaited task, on the thread that observed its completion
        yield return CreateCompletionAction(WhenAllPromiseTypeName);
        yield return CreateCompletionAction(WhenAllPromiseTResultTypeName);

        // Internal API behind TaskFactory.ContinueWhenAll, which counts completions the same way
        yield return CreateCompletionAction(CountdownPromiseTypeName);
        yield return CreateCompletionAction(CountdownPromiseTResultTypeName);
    }

    private static MethodDescriptor CreateCompletionAction(string declaringTypeName)
    {
        return new MethodDescriptor(
            MethodName: "Invoke",
            DeclaringTypeFullName: declaringTypeName,
            VersionDescriptor: null,
            SignatureDescriptor: new MethodSignatureDescriptor(
                CallingConvention: CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                ParametersCount: 1,
                ReturnType: ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_VOID),
                ArgumentTypeElements: [ArgumentTypeDescriptor.CreateClass(TaskTypeName)]),
            RewritingDescriptor: new MethodRewritingDescriptor(
                InjectHooks: true,
                InjectManagedWrapper: false,
                Arguments: [ObjectRefArg, CompletedTaskArg],
                ReturnValue: null,
                MethodEnterInterpretation: (ushort)RecordedEventType.TaskCompletionForwardToPromise,
                MethodExitInterpretation: null,
                EmitExitEvent: false));
    }
}
