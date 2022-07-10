using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using NetMQ;
using NetMQ.Sockets;
using SharpDetect.Common;
using SharpDetect.Common.Messages;

namespace SharpDetect.IntegrationTests.Mocks
{
    public class MockProfiler : IDisposable
    {
        private readonly IConfiguration configuration;
        private readonly int processId;
        private PushSocket notificationsSenderSocket;
        private SubscriberSocket requestsReceivingSocket;
        private PushSocket responseSendingSocket;
        private NetMQPoller requestsPoller;
        private bool isDisposed;
        private bool started;

        public MockProfiler(IConfiguration configuration, int processId)
        {
            this.configuration = configuration;
            this.processId = processId;
            this.notificationsSenderSocket = null!;
            this.requestsReceivingSocket = null!;
            this.responseSendingSocket = null!;
            this.requestsPoller = null!;
            this.started = false;
        }

        public void Start()
        {
            {
                // Notifications sender
                var protocol = configuration.GetRequiredSection(Constants.Communication.Notifications.Protocol).Value;
                var address = configuration.GetRequiredSection(Constants.Communication.Notifications.Address).Value;
                var port = configuration.GetRequiredSection(Constants.Communication.Notifications.Port).Value;
                var connectionString = $"{protocol}://{address}:{port}";
                notificationsSenderSocket = new PushSocket();
                notificationsSenderSocket.Connect(connectionString);
            }
            {
                // Response sender
                var protocol = configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Protocol).Value;
                var address = configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Address).Value;
                var port = configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Port).Value;
                var connectionString = $"{protocol}://{address}:{port}";
                responseSendingSocket = new PushSocket();
                responseSendingSocket.Connect(connectionString);
            }
            {
                // Requests sink
                var protocol = configuration.GetRequiredSection(Constants.Communication.Requests.Outbound.Protocol).Value;
                var address = configuration.GetRequiredSection(Constants.Communication.Requests.Outbound.Address).Value;
                var port = configuration.GetRequiredSection(Constants.Communication.Requests.Outbound.Port).Value;
                var connectionString = $"{protocol}://{address}:{port}";
                requestsReceivingSocket = new SubscriberSocket();
                requestsReceivingSocket.Connect(connectionString);
                requestsReceivingSocket.SubscribeToAnyTopic();
                requestsReceivingSocket.ReceiveReady += (sender, args) =>
                {
                    while (requestsReceivingSocket.HasIn)
                    {
                        // Receive request
                        requestsReceivingSocket.TryReceiveFrameBytes(out var topic);
                        requestsReceivingSocket.TryReceiveFrameBytes(out var data);
                        var request = RequestMessage.Parser.ParseFrom(data);

                        // Send response
                        var response = new NotifyMessage()
                        {
                            Response = new Response()
                            {
                                RequestId = request.RequestId,
                                Result = true
                            },
                            ProcessId = processId
                        };
                        responseSendingSocket!.SendFrame(response.ToByteArray());
                    }
                };
                requestsPoller = new NetMQPoller();
                requestsPoller.Add(requestsReceivingSocket);
                requestsPoller.RunAsync();
            }

            Task.Delay(500).Wait();
            started = true;
        }

        public void Send(NotifyMessage message)
        {
            if (!started)
                throw new InvalidOperationException("Start the profiler first!");

            notificationsSenderSocket.SendFrame(message.ToByteArray());
        }

        public void Dispose()
        {
            if (started && !isDisposed)
            {
                isDisposed = true;
                notificationsSenderSocket.Dispose();
                requestsPoller.Dispose();
                requestsReceivingSocket.Dispose();
                responseSendingSocket.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
