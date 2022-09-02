using dnlib.DotNet;
using SharpDetect.Common.Messages;

namespace SharpDetect.Common.Services
{
    public interface IRewritingMessageHub : INotificationsHandler
    {
        // Metadata creating
        event Action<(TypeInfo TypeInfo, RawEventInfo Info)> TypeInjected;
        event Action<(FunctionInfo FunctionInfo, MethodType Type, RawEventInfo Info)> MethodInjected;
        event Action<(FunctionInfo FunctionInfo, MDToken WrapperToken, RawEventInfo Info)> MethodWrapperInjected;

        // Metadata references
        event Action<(TypeInfo TypeInfo, RawEventInfo Info)> TypeReferenced;
        event Action<(FunctionInfo FunctionInfo, MethodType Type, RawEventInfo Info)> HelperMethodReferenced;
        event Action<(FunctionInfo Definition, FunctionInfo Reference, RawEventInfo Info)> WrapperMethodReferenced;
    }
}
