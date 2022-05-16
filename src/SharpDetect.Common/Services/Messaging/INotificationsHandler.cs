using SharpDetect.Common.Messages;

namespace SharpDetect.Common.Services
{
    public interface INotificationsHandler
    {
        bool CanHandle(NotifyMessage.PayloadOneofCase type);

        void Process(NotifyMessage message);
    }
}
