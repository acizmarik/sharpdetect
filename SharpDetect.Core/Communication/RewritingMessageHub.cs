using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services;

namespace SharpDetect.Core.Communication
{
    internal class RewritingMessageHub : MessageHubBase, IRewritingMessageHub
    {
        public event Action<(TypeInfo TypeInfo, EventInfo Info)>? TypeInjected;
        public event Action<(FunctionInfo FunctionInfo, MethodType Type, EventInfo Info)>? MethodInjected;
        public event Action<(FunctionInfo FunctionInfo, MDToken WrapperToken, EventInfo Info)>? MethodWrapperInjected;
        public event Action<(TypeInfo TypeInfo, EventInfo Info)>? TypeReferenced;
        public event Action<(FunctionInfo FunctionInfo, MethodType Type, EventInfo Info)>? HelperMethodReferenced;
        public event Action<(FunctionInfo Definition, FunctionInfo Reference, EventInfo Info)>? WrapperMethodReferenced;

        public RewritingMessageHub(ILoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<RewritingMessageHub>(), new[]
            {
                NotifyMessage.PayloadOneofCase.TypeInjected,
                NotifyMessage.PayloadOneofCase.MethodInjected,
                NotifyMessage.PayloadOneofCase.MethodWrapperInjected,
                NotifyMessage.PayloadOneofCase.TypeReferenced,
                NotifyMessage.PayloadOneofCase.HelperMethodReferenced,
                NotifyMessage.PayloadOneofCase.WrapperMethodReferenced,
            })
        {
        }

        public void Process(NotifyMessage message)
        {
            switch (message.PayloadCase)
            {
                case NotifyMessage.PayloadOneofCase.TypeInjected: DispatchTypeInjected(message); break;
                case NotifyMessage.PayloadOneofCase.MethodInjected: DispatchMethodInjected(message); break;
                case NotifyMessage.PayloadOneofCase.MethodWrapperInjected: DispatchMethodWrapperInjected(message); break;
                case NotifyMessage.PayloadOneofCase.TypeReferenced: DispatchTypeReferenced(message); break;
                case NotifyMessage.PayloadOneofCase.HelperMethodReferenced: DispatchHelperMethodReferenced(message); break;
                case NotifyMessage.PayloadOneofCase.WrapperMethodReferenced: DispatchWrapperMethodReferenced(message); break;

                default:
                    Logger.LogError("[{class}] Unrecognized message type: {type}!", nameof(RewritingMessageHub), message.PayloadCase);
                    throw new NotSupportedException("Provided message type is not supported.");
            }
        }

        private void DispatchTypeInjected(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var typeInjected = message.TypeInjected;
            var typeInfo = new TypeInfo(new(typeInjected.ModuleId), new(typeInjected.TypeToken));
            TypeInjected?.Invoke((typeInfo, info));
        }

        private void DispatchMethodInjected(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var methodInjected = message.MethodInjected;
            var functionInfo = new FunctionInfo(new(methodInjected.ModuleId), new(methodInjected.TypeToken), new(methodInjected.FunctionToken));
            MethodInjected?.Invoke((functionInfo, methodInjected.Type, info));
        }

        private void DispatchMethodWrapperInjected(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var methodWrapped = message.MethodWrapperInjected;
            var originalMethod = new FunctionInfo(new(methodWrapped.ModuleId), new(methodWrapped.TypeToken), new(methodWrapped.OriginalFunctionToken));
            MethodWrapperInjected?.Invoke((originalMethod, new(methodWrapped.WrapperFunctionToken), info));
        }

        private void DispatchTypeReferenced(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var typeReferenced = message.TypeReferenced;
            var typeInfo = new TypeInfo(new(typeReferenced.ModuleId), new(typeReferenced.TypeToken));
            TypeReferenced?.Invoke((typeInfo, info));
        }

        private void DispatchHelperMethodReferenced(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var helperMethodReferenced = message.HelperMethodReferenced;
            var type = helperMethodReferenced.Type;
            var functionInfo = new FunctionInfo(
                new(helperMethodReferenced.ModuleId),
                new(helperMethodReferenced.TypeToken),
                new(helperMethodReferenced.FunctionToken));
            HelperMethodReferenced?.Invoke((functionInfo, type, info));
        }

        private void DispatchWrapperMethodReferenced(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var wrapperMethodReferenced = message.WrapperMethodReferenced;
            var definition = new FunctionInfo(
                new(wrapperMethodReferenced.DefModuleId),
                new(wrapperMethodReferenced.DefTypeToken),
                new(wrapperMethodReferenced.DefFunctionToken));
            var reference = new FunctionInfo(
                new(wrapperMethodReferenced.RefModuleId),
                new(wrapperMethodReferenced.RefTypeToken),
                new(wrapperMethodReferenced.RefFunctionToken));
            WrapperMethodReferenced?.Invoke((definition, reference, info));
        }
    }
}
