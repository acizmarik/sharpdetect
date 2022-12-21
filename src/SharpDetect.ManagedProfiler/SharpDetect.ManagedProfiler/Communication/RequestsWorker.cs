using NetMQ;
using NetMQ.Sockets;
using SharpDetect.Common.Messages;

namespace SharpDetect.Profiler.Communication
{
    internal class RequestsWorker : ICommunicationWorker
    {
        public event Action<RequestMessage>? RequestReceived;
        private readonly string subscribeTopic;
        private readonly string connectionString;
        private readonly SubscriberSocket socket;
        private readonly NetMQPoller poller;
        private bool isDisposed;

        public RequestsWorker(string connectionString)
        {
            this.connectionString = connectionString;

            subscribeTopic = Environment.ProcessId.ToString();
            socket = new SubscriberSocket();
            poller = new NetMQPoller();
            socket.ReceiveReady += OnRequestReady;
        }

        private void OnRequestReady(object? _, NetMQSocketEventArgs e)
        {
            while (socket.HasIn)
            {
                // Read topic
                _ = socket.ReceiveFrameBytes();

                // Read payload
                var payload = socket.ReceiveFrameBytes();

                var request = RequestMessage.Parser.ParseFrom(payload)!;
                RequestReceived?.Invoke(request);
            }
        }

        public void Start()
        {
            socket.Subscribe(subscribeTopic);
            socket.Connect(connectionString);
            poller.Add(socket);
            poller.RunAsync();
        }

        public void Terminate()
        {
            poller.StopAsync();
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                poller.Dispose();
                socket.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
