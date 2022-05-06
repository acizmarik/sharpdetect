using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services.Endpoints;
using System.Collections.Concurrent;

namespace SharpDetect.Core.Communication.Endpoints
{
    internal class RequestServer : IRequestsProducer, IDisposable
    {
        public readonly IConfiguration Configuration;
        private readonly string outboundConnectionString;
        private readonly string inboundConnectionString;
        private readonly ILogger<RequestServer> logger;
        private readonly BlockingCollection<(byte[] Topic, byte[] Data)> outboundQueue;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<Response>> responsePromises;
        private readonly Thread outboundWorkerThread;
        private readonly Thread inboundWorkerThread;
        private volatile bool terminating;
        private bool isDisposed;

        public RequestServer(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            this.logger = loggerFactory.CreateLogger<RequestServer>();

            // Build inbound connection string
            var inProtocol = Configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Protocol).Value;
            var inAddress = Configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Address).Value;
            var inPort = Configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Port).Value;
            this.inboundConnectionString = $"{inProtocol}://{inAddress}:{inPort}";
            
            // Build outbound connection string
            var outProtocol = Configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Protocol).Value;
            var outAddress = Configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Address).Value;
            var outPort = Configuration.GetRequiredSection(Constants.Communication.Requests.Inbound.Port).Value;
            this.outboundConnectionString = $"{outProtocol}://{outAddress}:{outPort}";
            
            this.inboundWorkerThread = new Thread(InboundCommunicationLoop);
            this.outboundWorkerThread = new Thread(OutboundCommunicationLoop);
            this.outboundQueue = new BlockingCollection<(byte[] Topic, byte[] Data)>();
            this.responsePromises = new ConcurrentDictionary<int, TaskCompletionSource<Response>>();
        }

        private void OutboundCommunicationLoop()
        {
            using (var socket = new PublisherSocket(outboundConnectionString))
            {
                logger.LogDebug("[{class}] Opened connection for profiler requests on {connectionString}.", nameof(RequestServer), outboundConnectionString);

                // Send requests and wait for promises async
                foreach (var (topic, data) in outboundQueue.GetConsumingEnumerable())
                {
                    socket
                        // Topic: process identification
                        .SendMoreFrame(topic)
                        // Data: rewriting request
                        .SendFrame(data);
                }
            }

            logger.LogDebug("[{class}] Closed connection for profiler requests on {connectionString}.", nameof(RequestServer), inboundConnectionString);
        }

        private void InboundCommunicationLoop()
        {
            var timeout = TimeSpan.FromSeconds(3);
            using (var socket = new SubscriberSocket(inboundConnectionString))
            {
                logger.LogDebug("[{class}] Opened connection for profiler request responses on {connectionString}.", nameof(RequestServer), inboundConnectionString);
                
                // We are actually listening to all topics (processing message from every process)
                socket.SubscribeToAnyTopic();

                // Wait for request responses
                while (!terminating)
                {
                    if (!socket.TryReceiveFrameBytes(timeout, out var topicData))
                    {
                        // We might have been terminated in the meantime
                        continue;
                    }

                    var topic = BitConverter.ToInt32(topicData);
                    var data = NotifyMessage.Parser.ParseFrom(socket.ReceiveFrameBytes());

                    // Mark request as finished
                    responsePromises.Remove(topic, out var promise);
                    promise!.SetResult(data.Response);                    
                }
            }

            logger.LogDebug("[{class}] Closed connection for profiler request responses on {connectionString}.", nameof(RequestServer), inboundConnectionString);
        }

        public Task<Response> SendAsync(int processId, RequestMessage message)
        {
            var marshalled = message.ToByteArray();
            var promise = new TaskCompletionSource<Response>();

            // Create a promise about upcomming response
            responsePromises.TryAdd(processId, promise);
            // Enqueue marshalled message for sending
            outboundQueue.Add((BitConverter.GetBytes(processId), marshalled));

            return promise.Task;
        }

        public void Start()
        {
            if (inboundWorkerThread.ThreadState == ThreadState.Unstarted)
                inboundWorkerThread.Start();
            if (outboundWorkerThread.ThreadState == ThreadState.Unstarted)
                outboundWorkerThread.Start();
        }

        public void Stop()
        {
            terminating = true;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                terminating = true;
                outboundQueue.CompleteAdding();
                outboundWorkerThread.Join();
                inboundWorkerThread.Join();
                GC.SuppressFinalize(this);
            }
        }
    }
}
