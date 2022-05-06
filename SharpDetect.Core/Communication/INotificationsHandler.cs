using SharpDetect.Common.Messages;

namespace SharpDetect.Core.Communication
{
    internal interface INotificationsHandler
    {
        bool CanHandle(NotifyMessage.PayloadOneofCase type);

        void Process(NotifyMessage message);
    }
}
