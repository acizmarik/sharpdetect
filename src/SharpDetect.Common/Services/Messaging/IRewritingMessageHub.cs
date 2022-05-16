using dnlib.DotNet;
using SharpDetect.Common.Messages;

namespace SharpDetect.Common.Services
{
    public interface IRewritingMessageHub : INotificationsHandler
    {
        // Metadata creating
        event Action<(TypeInfo TypeInfo, EventInfo Info)> TypeInjected;
        event Action<(FunctionInfo FunctionInfo, MethodType Type, EventInfo Info)> MethodInjected;
        event Action<(FunctionInfo FunctionInfo, MDToken WrapperToken, EventInfo Info)> MethodWrapperInjected;

        // Metadata references
        event Action<(TypeInfo TypeInfo, EventInfo Info)> TypeReferenced;
        event Action<(FunctionInfo FunctionInfo, MethodType Type, EventInfo Info)> HelperMethodReferenced;
        event Action<(FunctionInfo Definition, FunctionInfo Reference, EventInfo Info)> WrapperMethodReferenced;
    }
}
