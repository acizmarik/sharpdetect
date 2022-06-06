using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using NetMQ;
using NetMQ.Sockets;
using SharpDetect.Common;
using SharpDetect.Common.Messages;

namespace SharpDetect.IntegrationTests.Mocks
{
    internal class MockProfiler : IDisposable
    {
        private readonly PushSocket socket;
        private bool isDisposed;

        public MockProfiler(IConfiguration configuration)
        {
            var protocol = configuration.GetRequiredSection(Constants.Communication.Notifications.Protocol).Value;
            var address = configuration.GetRequiredSection(Constants.Communication.Notifications.Address).Value;
            var port = configuration.GetRequiredSection(Constants.Communication.Notifications.Port).Value;
            var connectionString = $"{protocol}://{address}:{port}";
            socket = new PushSocket(connectionString);
        }

        public void Send(NotifyMessage message)
        {
            socket.SendFrame(message.ToByteArray());
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                socket.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
