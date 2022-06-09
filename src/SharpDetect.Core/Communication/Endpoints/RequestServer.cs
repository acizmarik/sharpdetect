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
        private readonly BlockingCollection<(int Topic, byte[] Data)> outboundQueue;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<Response>> responsePromises;
        private readonly ConcurrentDictionary<int, (bool State, TaskCompletionSource<Response>? Promise)> establishedConnections;
        private readonly TimeSpan connectionDiscoveryRetryDelay;
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
            var outProtocol = Configuration.GetRequiredSection(Constants.Communication.Requests.Outbound.Protocol).Value;
            var outAddress = Configuration.GetRequiredSection(Constants.Communication.Requests.Outbound.Address).Value;
            var outPort = Configuration.GetRequiredSection(Constants.Communication.Requests.Outbound.Port).Value;
            this.outboundConnectionString = $"{outProtocol}://{outAddress}:{outPort}";
            
            this.inboundWorkerThread = new Thread(InboundCommunicationLoop);
            this.outboundWorkerThread = new Thread(OutboundCommunicationLoop);
            this.outboundQueue = new BlockingCollection<(int Topic, byte[] Data)>();
            this.responsePromises = new ConcurrentDictionary<int, TaskCompletionSource<Response>>();
            this.establishedConnections = new ConcurrentDictionary<int, (bool State, TaskCompletionSource<Response>? Promise)>();
            this.connectionDiscoveryRetryDelay = TimeSpan.FromMilliseconds(100);
        }

        private void OutboundCommunicationLoop()
        {
            void EnsureConnectionEstablished(PublisherSocket socket, int topic)
            {
                if (!establishedConnections.TryGetValue(topic, out var info) || !info.State)
                {
                    var topicMarshaled = BitConverter.GetBytes(topic);
                    var pingRequestMarshaled = new RequestMessage() { RequestId = 0, Ping = new Request_Ping() }.ToByteArray();
                    var taskCompletionSource = new TaskCompletionSource<Response>();
                    establishedConnections.TryAdd(topic, (false, taskCompletionSource));

                    logger.LogDebug("[{class}] Waiting for reliable PUB-SUB connection.", nameof(RequestServer));
                    while (!info.State)
                    {
                        socket
                            .SendMoreFrame(topicMarshaled)
                            .SendFrame(pingRequestMarshaled);
                        if (taskCompletionSource.Task.Wait(connectionDiscoveryRetryDelay))
                        {
                            // The other side responded - the connection is established
                            info.State = true;
                            logger.LogDebug("[{class}] Reliable PUB-SUB connection established.", nameof(RequestServer));
                        }
                    }
                }
            }

            using (var socket = new PublisherSocket(outboundConnectionString))
            {
                logger.LogDebug("[{class}] Opened connection for profiler requests on {connectionString}.", nameof(RequestServer), outboundConnectionString);

                // Send requests and wait for promises async
                foreach (var (topic, data) in outboundQueue.GetConsumingEnumerable())
                {
                    /*  This is a work-around for a well-known ZMQ issue with establishing connections for PUB-SUB
                     *  We just ping subscriber until it responds using the second channel */
                    EnsureConnectionEstablished(socket, topic);

                    socket
                        // Topic: process identification
                        .SendMoreFrame(BitConverter.GetBytes(topic))
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
                    establishedConnections.TryGetValue(topic, out var connectionInfo);
                    if (data.Response.RequestId == 0)
                    {
                        if (connectionInfo.Promise != null)
                        {
                            // Connection established
                            establishedConnections[topic] = (true, null);
                            connectionInfo.Promise.SetResult(data.Response);
                        }
                        else
                        {
                            // Repeated message - do nothing
                        }
                    }
                    else
                    {
                        // Regular notification
                        responsePromises.Remove(topic, out var promise);
                        promise!.SetResult(data.Response);
                    }        
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
            outboundQueue.Add((processId, marshalled));

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
