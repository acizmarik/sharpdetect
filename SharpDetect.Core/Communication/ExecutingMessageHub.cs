using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Utilities;

namespace SharpDetect.Core.Communication
{
    internal class ExecutingMessageHub : MessageHubBase, IExecutingMessageHub
    {
        public event Action<(ulong Identifier, bool IsWrite, EventInfo Info)>? FieldAccessed;
        public event Action<(UIntPtr Instance, EventInfo Info)>? FieldInstanceAccessed;
        public event Action<(ulong Identifier, bool IsWrite, EventInfo Info)>? ArrayElementAccessed;
        public event Action<(UIntPtr Instance, EventInfo Info)>? ArrayInstanceAccessed;
        public event Action<(int Index, EventInfo Info)>? ArrayIndexAccessed;
        public event Action<(FunctionInfo Function, (ushort, IValueOrPointer)[]? Arguments, EventInfo Info)>? MethodCalled;
        public event Action<(FunctionInfo Function, IValueOrPointer? ReturnValue, (ushort, IValueOrPointer)[]? ByRefArguments, EventInfo Info)>? MethodReturned;
        public event Action<(FunctionInfo Function, UIntPtr Instance, EventInfo Info)>? LockAcquireAttempted;
        public event Action<(FunctionInfo Function, bool IsSuccess, EventInfo Info)>? LockAcquireReturned;
        public event Action<(FunctionInfo Function, UIntPtr Instance, EventInfo Info)>? LockReleaseCalled;
        public event Action<(FunctionInfo Function, EventInfo Info)>? LockReleaseReturned;
        public event Action<(FunctionInfo Function, UIntPtr Instance, EventInfo Info)>? ObjectWaitAttempted;
        public event Action<(FunctionInfo Function, bool IsSuccess, EventInfo Info)>? ObjectWaitReturned;
        public event Action<(FunctionInfo Function, bool IsPulseAll, UIntPtr Instance, EventInfo Info)>? ObjectPulseCalled;
        public event Action<(FunctionInfo Function, bool IsPulseAll, EventInfo Info)>? ObjectPulseReturned;

        private readonly IMetadataContext metadataContext;
        private readonly IMethodDescriptorRegistry methodRegistry;

        public ExecutingMessageHub(IMetadataContext metadataContext, IMethodDescriptorRegistry methodRegistry, ILoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<ProfilingMessageHub>(), new[]
            {
                NotifyMessage.PayloadOneofCase.MethodCalled,
                NotifyMessage.PayloadOneofCase.MethodReturned
            })
        {
            this.metadataContext = metadataContext;
            this.methodRegistry = methodRegistry;
        }

        public void Process(NotifyMessage message)
        {
            switch (message.PayloadCase)
            {
                case NotifyMessage.PayloadOneofCase.MethodCalled: DispatchMethodCalled(message); break;
                case NotifyMessage.PayloadOneofCase.MethodReturned: DispatchMethodReturned(message); break;

                default:
                    Logger.LogError("[{class}] Unrecognized message type: {type}!", nameof(ExecutingMessageHub), message.PayloadCase);
                    throw new NotSupportedException("Provided message type is not supported.");
            }
        }

        private void DispatchMethodCalled(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var methodCalledMessage = message.MethodCalled;
            var moduleInfo = new ModuleInfo(new(methodCalledMessage.ModuleId));
            var functionInfo = new FunctionInfo(
                new(methodCalledMessage.ModuleId),
                new(methodCalledMessage.TypeToken),
                new(methodCalledMessage.FunctionToken));

            // Resolve method definition
            var metadataResolver = metadataContext.GetResolver(info.ProcessId);
            if (!metadataResolver.TryGetMethodDef(functionInfo, moduleInfo, out var methodDef))
            {
                Logger.LogWarning("[{class}] Could not resolve method with token: {token}!", nameof(ExecutingMessageHub), functionInfo.FunctionToken);
                return;
            }

            // Resolve method call arguments
            var rawArguments = (!methodCalledMessage.ArgumentValues.IsEmpty) ?
                ArgumentsHelper.ParseArguments(
                    methodDef, 
                    methodCalledMessage.ArgumentValues.Span, 
                    methodCalledMessage.ArgumentOffsets.Span) :
                Array.Empty<(ushort Index, IValueOrPointer Argument)>();

            // Resolve method interpretation
            if (methodRegistry.TryGetMethodInterpretationData(methodDef, out var interpretationData))
            {
                // Raise more specific events (based on the method interpretation)
                switch (interpretationData.Interpretation)
                {
                    // Lock acquire calls
                    case MethodInterpretation.LockTryAcquire:
                    case MethodInterpretation.LockBlockingAcquire:
                        {
                            var pointer = rawArguments[0].Argument.Pointer!.Value;
                            LockAcquireAttempted?.Invoke((functionInfo, pointer, info));
                            break;
                        }
                    // Lock release calls
                    case MethodInterpretation.LockRelease:
                        {
                            var pointer = rawArguments[0].Argument.Pointer!.Value;
                            LockReleaseCalled?.Invoke((functionInfo, pointer, info));
                            break;
                        }
                    // Signal wait calls
                    case MethodInterpretation.SignalTryWait:
                    case MethodInterpretation.SignalBlockingWait:
                        {
                            var pointer = rawArguments[0].Argument.Pointer!.Value;
                            ObjectWaitAttempted?.Invoke((functionInfo, rawArguments[0].Argument.Pointer!.Value, info));
                            break;
                        }
                    // Signal pulse calls
                    case MethodInterpretation.SignalPulseOne:
                    case MethodInterpretation.SignalPulseAll:
                        {
                            var pointer = rawArguments[0].Argument.Pointer!.Value;
                            var isPulseAll = interpretationData.Interpretation == MethodInterpretation.SignalPulseAll;
                            ObjectPulseCalled?.Invoke((functionInfo, isPulseAll, pointer, info));
                            break;
                        }
                    // Fields
                    case MethodInterpretation.FieldAccess:
                        {
                            var isWrite = (bool)rawArguments[0].Argument.BoxedValue!;
                            var identifier = (ulong)rawArguments[1].Argument.BoxedValue!;
                            FieldAccessed?.Invoke((identifier, isWrite, info));
                            break;
                        }
                    case MethodInterpretation.FieldInstanceAccess:
                        {
                            var pointer = rawArguments[0].Argument.Pointer!.Value;
                            FieldInstanceAccessed?.Invoke((pointer, info));
                            break;
                        }
                    // Arrays
                    case MethodInterpretation.ArrayElementAccess:
                        {
                            var isWrite = (bool)rawArguments[0].Argument.BoxedValue!;
                            var identifier = (ulong)rawArguments[1].Argument.BoxedValue!;
                            ArrayElementAccessed?.Invoke((identifier, isWrite, info));
                            break;
                        }
                    case MethodInterpretation.ArrayInstanceAccess:
                        {
                            var pointer = rawArguments[0].Argument.Pointer!.Value;
                            ArrayInstanceAccessed?.Invoke((pointer, info));
                            break;
                        }
                    case MethodInterpretation.ArrayIndexAccess:
                        {
                            var index = (int)rawArguments[0].Argument.BoxedValue!;
                            ArrayIndexAccessed?.Invoke((index, info));
                            break;
                        }
                    default:
                        var interpretationStringified = Enum.GetName(interpretationData.Interpretation);
                        Logger.LogError("[{class}] Unrecognized method interpretation type: {interp}!", nameof(ExecutingMessageHub), interpretationStringified);
                        throw new NotSupportedException("Provided method interpretation type is not supported.");
                }
            }

            // Raise generic method called event as well
            MethodCalled?.Invoke((functionInfo, rawArguments, info));
        }

        private void DispatchMethodReturned(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var methodReturnedMessage = message.MethodReturned;
            var moduleInfo = new ModuleInfo(new(methodReturnedMessage.ModuleId));
            var functionInfo = new FunctionInfo(
                new(methodReturnedMessage.ModuleId),
                new(methodReturnedMessage.TypeToken),
                new(methodReturnedMessage.FunctionToken));

            // Resolve method definition
            var metadataResolver = metadataContext.GetResolver(info.ProcessId);
            if (!metadataResolver.TryGetMethodDef(functionInfo, moduleInfo, out var methodDef))
            {
                Logger.LogWarning("[{class}] Could not resolve method with token: {token}!", nameof(ExecutingMessageHub), functionInfo.FunctionToken);
                return;
            }

            // Resolve return value
            var returnValue = (!methodReturnedMessage.ReturnValue.IsEmpty) ?
                ArgumentsHelper.ParseArgument(methodDef.ReturnType, methodReturnedMessage.ReturnValue.Span) :
                null as IValueOrPointer;

            // Resolve byRef arguments
            var byRefArguments = (!methodReturnedMessage.ByRefArgumentValues.IsEmpty) ?
                ArgumentsHelper.ParseArguments(
                    methodDef,
                    methodReturnedMessage.ByRefArgumentValues.Span,
                    methodReturnedMessage.ByRefArgumentOffsets.Span) :
                Array.Empty<(ushort Index, IValueOrPointer Argument)>();

            // Resolve method interpretation
            if (methodRegistry.TryGetMethodInterpretationData(methodDef, out var interpretationData))
            {
                switch (interpretationData.Interpretation)
                {
                    // Lock acquire returns
                    case MethodInterpretation.LockTryAcquire:
                    case MethodInterpretation.LockBlockingAcquire:
                        {
                            var isSuccess = interpretationData.Checker(returnValue, byRefArguments);
                            LockAcquireReturned?.Invoke((functionInfo, isSuccess, info));
                            break;
                        }
                    // Lock release returns
                    case MethodInterpretation.LockRelease:
                        {
                            LockReleaseReturned?.Invoke((functionInfo, info));
                            break;
                        }
                    // Signal wait returns
                    case MethodInterpretation.SignalTryWait:
                    case MethodInterpretation.SignalBlockingWait:
                        {
                            var isSuccess = interpretationData.Checker(returnValue, byRefArguments);
                            ObjectWaitReturned?.Invoke((functionInfo, isSuccess, info));
                            break;
                        }
                    // Signal pulse returns
                    case MethodInterpretation.SignalPulseOne:
                    case MethodInterpretation.SignalPulseAll:
                        {
                            var isPulseAll = interpretationData.Interpretation == MethodInterpretation.SignalPulseAll;
                            ObjectPulseReturned?.Invoke((functionInfo, isPulseAll, info));
                            break;
                        }
                }
            }

            // Raise generic method returned event as well
            MethodReturned?.Invoke((functionInfo, returnValue, byRefArguments, info));
        }
    }
}
