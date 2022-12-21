using NetMQ;
using NetMQ.Sockets;
using SharpDetect.Profiler.Logging;

namespace SharpDetect.Profiler.Communication
{
    internal abstract class RelayWorkerBase : ICommunicationWorker
    {
        private readonly string inConnectionString;
        private readonly string outConnectionString;
        private readonly PushSocket outSocket;
        private readonly PullSocket inSocket;
        private readonly NetMQPoller poller;
        private bool isDisposed;

        public RelayWorkerBase(string outConnectionString, string inConnectionString)
        {
            this.outConnectionString = outConnectionString;
            this.inConnectionString = inConnectionString;
            
            outSocket = new PushSocket();
            inSocket = new PullSocket();
            poller = new NetMQPoller();
            inSocket.ReceiveReady += OnNotificationReady;
        }

        protected virtual void OnNotificationReady(object? _, NetMQSocketEventArgs e)
        {
            while (inSocket.HasIn)
            {
                var payload = inSocket.ReceiveFrameBytes();
                outSocket.SendFrame(payload);
            }
        }

        public void Start()
        {
            outSocket.Connect(outConnectionString);
            inSocket.Bind(inConnectionString);
            poller.Add(inSocket);
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
                inSocket.Dispose();
                outSocket.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
