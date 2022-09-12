using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services;
using SharpDetect.Core.Utilities;

namespace SharpDetect.Core.Communication
{
    internal class ExecutingMessageHub : MessageHubBase, IExecutingMessageHub
    {
        public event Action<(FunctionInfo Function, RawArgumentsList? Arguments, RawEventInfo Info)>? MethodCalled;
        public event Action<(FunctionInfo Function, RawReturnValue? ReturnValue, RawArgumentsList? ByRefArguments, RawEventInfo Info)>? MethodReturned;

        public ExecutingMessageHub(ILoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<ProfilingMessageHub>(), new[]
            {
                NotifyMessage.PayloadOneofCase.MethodCalled,
                NotifyMessage.PayloadOneofCase.MethodReturned
            })
        {

        }

        public void Process(NotifyMessage message)
        {
            switch (message.PayloadCase)
            {
                case NotifyMessage.PayloadOneofCase.MethodCalled: DispatchMethodCalled(message); break;
                case NotifyMessage.PayloadOneofCase.MethodReturned: DispatchMethodReturned(message); break;

                default:
                    Logger.LogError("[{class}] Unrecognized message type: {type}.", nameof(ExecutingMessageHub), message.PayloadCase);
                    throw new NotSupportedException("Provided message type is not supported.");
            }
        }

        private void DispatchMethodCalled(NotifyMessage message)
        {
            var info = CreateEventInfo(message);
            var methodCalledMessage = message.MethodCalled;
            var functionInfo = new FunctionInfo(
                new(methodCalledMessage.ModuleId),
                new(methodCalledMessage.TypeToken),
                new(methodCalledMessage.FunctionToken));

            // Raise generic method called
            var argslist = (methodCalledMessage.ArgumentValues.Length != 0) ? 
                new RawArgumentsList(methodCalledMessage.ArgumentValues, methodCalledMessage.ArgumentOffsets) : null as RawArgumentsList?;
            MethodCalled?.Invoke((functionInfo, argslist, info));
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

            // Raise generic method returned
            var returnValue = (methodReturnedMessage.ReturnValue.Length != 0) ? 
                new RawReturnValue(methodReturnedMessage.ReturnValue) : null as RawReturnValue?;
            var argslist = (methodReturnedMessage.ByRefArgumentValues.Length != 0) ?
                new RawArgumentsList(methodReturnedMessage.ByRefArgumentValues, methodReturnedMessage.ByRefArgumentOffsets) : null as RawArgumentsList?;
            MethodReturned?.Invoke((functionInfo, returnValue, argslist, info));
        }
    }
}
