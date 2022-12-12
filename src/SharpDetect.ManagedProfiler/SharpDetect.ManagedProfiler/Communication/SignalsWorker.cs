using SharpDetect.Common.Messages;
using NetMQ.Sockets;
using Google.Protobuf;
using NetMQ;

namespace SharpDetect.Profiler.Communication
{
    internal class SignalsWorker : ICommunicationWorker
    {
        private readonly string connectionString;
        private readonly PushSocket socket;
        private readonly Thread thread;
        private volatile bool shouldTerminate;
        private bool isDisposed;

        public SignalsWorker(string connectionString)
        {
            this.connectionString = connectionString;
            this.socket = new PushSocket();
            this.thread = new Thread(ThreadLoop);
        }

        private void ThreadLoop()
        {
            // Prepare static array of message bytes since it does not change
            var message = new NotifyMessage() { Heartbeat = new Notify_Heartbeat() };
            var messageBytes = message.ToByteArray();

            while (!shouldTerminate)
            {
                // Send heartbeat
                socket.SendFrame(messageBytes);

                // Put thread to sleep until next heartbeat
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        public void Start()
        {
            socket.Connect(connectionString);
            thread.Start();
        }

        public void Terminate()
        {
            shouldTerminate = true;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                if (thread.ThreadState != ThreadState.Unstarted)
                    thread.Join();
                socket.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
