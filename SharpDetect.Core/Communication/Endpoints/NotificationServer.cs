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
        private readonly string connectionString;
        private readonly IRewritingMessageHub profilingMessageHub;
        private readonly IRewritingMessageHub rewritingMessageHub;
        private readonly IExecutingMessageHub executingMessageHub;
        private readonly ILogger<NotificationServer> logger;
        private readonly PullSocket socket;
        private readonly NetMQPoller poller;
        private bool isDisposed;

        public NotificationServer(IConfiguration configuration, IRewritingMessageHub profilingHub, IRewritingMessageHub rewritingHub, IExecutingMessageHub executingHub, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            this.profilingMessageHub = profilingHub;
            this.rewritingMessageHub = rewritingHub;
            this.executingMessageHub = executingHub;
            this.logger = loggerFactory.CreateLogger<NotificationServer>();

            // Initialize messaging
            var protocol = Configuration.GetRequiredSection(Constants.Communication.Notifications.Protocol).Value;
            var address = Configuration.GetRequiredSection(Constants.Communication.Notifications.Address).Value;
            var port = Configuration.GetRequiredSection(Constants.Communication.Notifications.Port).Value;
            this.connectionString = $"{protocol}://{address}:{port}";
            this.poller = new NetMQPoller();
            this.socket = new PullSocket(connectionString);
            this.poller.Add(socket);
        }

        private void ReceiveProfilerMessage(object _, NetMQSocketEventArgs e)
        {
            while (socket.HasIn)
            {
                // Read next message
                var payload = socket.ReceiveFrameBytes();
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

        public void Start()
        {
            if (!poller.IsRunning)
            {
                poller.RunAsync();
                logger.LogDebug("[{class}] Opened connection for profiler notifications on {connectionString}.", nameof(NotificationServer), connectionString);
            }
        }

        public void Stop()
        {
            if (poller.IsRunning)
            {
                poller.Stop();
                logger.LogDebug("[{class}] Closed connection for profiler notifications on {connectionString}.", nameof(NotificationServer), connectionString);
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                poller.Stop();
                poller.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
