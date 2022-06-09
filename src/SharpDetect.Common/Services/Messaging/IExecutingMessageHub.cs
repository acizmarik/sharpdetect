using Google.Protobuf;

namespace SharpDetect.Common.Services
{
    public record struct RawArgumentsList(ByteString ArgValues, ByteString ArgOffsets);
    public record struct RawReturnValue(ByteString ReturnValue);

    public interface IExecutingMessageHub : INotificationsHandler
    {
        event Action<(FunctionInfo Function, RawArgumentsList? Arguments, EventInfo Info)> MethodCalled;
        event Action<(FunctionInfo Function, RawReturnValue? ReturnValue, RawArgumentsList? ByRefArguments, EventInfo Info)> MethodReturned;
    }
}
