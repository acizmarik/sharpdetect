// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Endpoints;

namespace SharpDetect.Core.Communication.Endpoints
{
    internal class NotificationServer : INotificationsConsumer, IDisposable
    {
        public readonly IConfiguration Configuration;
        private readonly string notificationsEndpointConnectionString;
        private readonly string signalsEndpointConnectionString;
        private readonly IProfilingMessageHub profilingMessageHub;
        private readonly IRewritingMessageHub rewritingMessageHub;
        private readonly IExecutingMessageHub executingMessageHub;
        private readonly ILogger<NotificationServer> logger;
        private readonly PullSocket notificationSocket;
        private readonly PullSocket signalSocket;
        private readonly NetMQPoller poller;
        private bool isDisposed;

        public NotificationServer(IConfiguration configuration, IProfilingMessageHub profilingHub, IRewritingMessageHub rewritingHub, IExecutingMessageHub executingHub, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            this.profilingMessageHub = profilingHub;
            this.rewritingMessageHub = rewritingHub;
            this.executingMessageHub = executingHub;
            this.logger = loggerFactory.CreateLogger<NotificationServer>();

            // Initialize messaging
            var notificationsProtocol = Configuration.GetRequiredSection(Constants.Communication.Notifications.Protocol).Value;
            var notificationsAddress = Configuration.GetRequiredSection(Constants.Communication.Notifications.Address).Value;
            var notificationsPort = Configuration.GetRequiredSection(Constants.Communication.Notifications.Port).Value;
            this.notificationsEndpointConnectionString = $"{notificationsProtocol}://{notificationsAddress}:{notificationsPort}";
            var signalsProtocol = Configuration.GetRequiredSection(Constants.Communication.Signals.Protocol).Value;
            var signalsAddress = Configuration.GetRequiredSection(Constants.Communication.Signals.Address).Value;
            var signalsPort = Configuration.GetRequiredSection(Constants.Communication.Signals.Port).Value;
            this.signalsEndpointConnectionString = $"{signalsProtocol}://{signalsAddress}:{signalsPort}";
            this.poller = new NetMQPoller();

            // Setup notifications socket
            this.notificationSocket = new PullSocket();
            notificationSocket.Bind(notificationsEndpointConnectionString);
            this.poller.Add(notificationSocket);
            this.notificationSocket.ReceiveReady += ReceiveProfilerMessage;

            // Setup signals socket
            this.signalSocket = new PullSocket();
            signalSocket.Bind(signalsEndpointConnectionString);
            this.poller.Add(signalSocket);
            this.signalSocket.ReceiveReady += ReceiveProfilerSignal;
        }

        private void ReceiveProfilerMessage(object? _, NetMQSocketEventArgs e)
        {
            while (notificationSocket.HasIn)
            {
                // Read next message
                var payload = notificationSocket.ReceiveFrameBytes();
                var message = NotifyMessage.Parser.ParseFrom(payload);

                // Relay to handlers
                if (profilingMessageHub.CanHandle(message.PayloadCase))
                    profilingMessageHub.Process(message);
                if (rewritingMessageHub.CanHandle(message.PayloadCase))
                    rewritingMessageHub.Process(message);
                if (executingMessageHub.CanHandle(message.PayloadCase))
                    executingMessageHub.Process(message);
            }
        }

        private void ReceiveProfilerSignal(object? _, NetMQSocketEventArgs e)
        {
            while (signalSocket.HasIn)
            {
                // Read next message
                var payload = signalSocket.ReceiveFrameBytes();
                var message = NotifyMessage.Parser.ParseFrom(payload);
                profilingMessageHub.Process(message);
            }
        }

        public void Start()
        {
            if (!poller.IsRunning)
            {
                poller.RunAsync();
                logger.LogDebug("[{class}] Opened connection for profiler signals on {connectionString}.", nameof(NotificationServer), signalsEndpointConnectionString);
                logger.LogDebug("[{class}] Opened connection for profiler notifications on {connectionString}.", nameof(NotificationServer), notificationsEndpointConnectionString);
            }
        }

        public void Stop()
        {
            if (poller.IsRunning)
            {
                poller.Stop();
                logger.LogDebug("[{class}] Closed connection for profiler signals on {connectionString}.", nameof(NotificationServer), signalsEndpointConnectionString);
                logger.LogDebug("[{class}] Closed connection for profiler notifications on {connectionString}.", nameof(NotificationServer), notificationsEndpointConnectionString);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                poller.Stop();
                poller.Dispose();
                notificationSocket.Dispose();
                signalSocket.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
