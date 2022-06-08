﻿using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using NetMQ;
using NetMQ.Sockets;
using SharpDetect.Common;
using SharpDetect.Common.Messages;

namespace SharpDetect.IntegrationTests.Mocks
{
    public class MockProfiler : IDisposable
    {
        private readonly PushSocket notificationsSenderSocket;
        private readonly SubscriberSocket requestsReceivingSocket;
        private readonly PublisherSocket responseSendingSocket;
        private readonly NetMQPoller requestsPoller;
        private bool isDisposed;

        public MockProfiler(IConfiguration configuration)
        {
            {
                // Notifications sender
                var protocol = configuration.GetRequiredSection(Constants.Communication.Notifications.Protocol).Value;
                var address = configuration.GetRequiredSection(Constants.Communication.Notifications.Address).Value;
                var port = configuration.GetRequiredSection(Constants.Communication.Notifications.Port).Value;
                var connectionString = $"{protocol}://{address}:{port}";
                notificationsSenderSocket = new PushSocket(connectionString);
            }
            {
                // Requests sink
                var protocol = configuration.GetRequiredSection(Constants.Communication.Requests.Outbound.Protocol).Value;
                var address = configuration.GetRequiredSection(Constants.Communication.Requests.Outbound.Address).Value;
                var port = configuration.GetRequiredSection(Constants.Communication.Requests.Outbound.Port).Value;
                var connectionString = $"{protocol}://{address}:{port}";
                requestsReceivingSocket = new SubscriberSocket(connectionString);
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
                        var response = new Response() { RequestId = request.RequestId, Result = true };
                        responseSendingSocket!.SendMoreFrame(topic!);
                        responseSendingSocket!.SendFrame(response.ToByteArray());
                    }
                };
                requestsPoller = new NetMQPoller();
                requestsPoller.Add(requestsReceivingSocket);
                requestsPoller.RunAsync();
            }
            {
                // Response sender
                var protocol = configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Protocol).Value;
                var address = configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Address).Value;
                var port = configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Port).Value;
                var connectionString = $"{protocol}://{address}:{port}";
                responseSendingSocket = new PublisherSocket(connectionString);
            }
        }

        public void Send(NotifyMessage message)
        {
            notificationsSenderSocket.SendFrame(message.ToByteArray());
        }

        public void Dispose()
        {
            if (!isDisposed)
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