using Google.Protobuf;
using NetMQ;
using NetMQ.Sockets;
using SharpDetect.Common.Messages;
using SharpDetect.Profiler.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Profiler.Communication
{
    internal class MessagingClient : IDisposable
    {
        private const string signalsProtocolEnvironmentKey = "SHARPDETECT_Signals_Protocol";
        private const string notificationsProtocolEnvironmentKey = "SHARPDETECT_Notifications_Protocol";
        private const string requestsProtocolEnvironmentKey = "SHARPDETECT_Requests_Protocol";
        private const string responsesProtocolEnvironmentKey = "SHARPDETECT_Responses_Protocol";
        private const string signalsAddressEnvironmentKey = "SHARPDETECT_Signals_Address";
        private const string notificationsAddressEnvironmentKey = "SHARPDETECT_Notifications_Address";
        private const string requestsAddressEnvironmentKey = "SHARPDETECT_Requests_Address";
        private const string responsesAddressEnvironmentKey = "SHARPDETECT_Responses_Address";
        private const string signalsPortEnvironmentKey = "SHARPDETECT_Signals_Port";
        private const string notificationsPortEnvironmentKey = "SHARPDETECT_Notifications_Port";
        private const string requestsPortEnvironmentKey = "SHARPDETECT_Requests_Port";
        private const string responsesPortEnvironmentKey = "SHARPDETECT_Responses_Port";
        private readonly ConcurrentDictionary<ulong, TaskCompletionSource<RequestMessage>> promises;
        private readonly string signalsConnectionString;
        private readonly string notificationsConnectionString;
        private readonly string requestsConnectionString;
        private readonly string responsesConnectionString;
        private readonly SignalsWorker signalsWorker;
        private readonly NotificationsWorker notificationsWorker;
        private readonly RequestsWorker requestsWorker;
        private readonly ResponsesWorker responsesWorker;
        private readonly int processId;
        private ulong notificationId;
        private bool isDisposed;

        public MessagingClient()
        {
            [DoesNotReturn]
            static void ThrowInvalidConfiguration(string argument)
                => throw new Exception($"Invalid configuration: could not build {argument}");

            // Prepare configuration strings
            if (!BuildConnectionString(signalsProtocolEnvironmentKey, signalsAddressEnvironmentKey, signalsPortEnvironmentKey, out signalsConnectionString!))
                ThrowInvalidConfiguration(nameof(signalsConnectionString));
            if (!BuildConnectionString(notificationsProtocolEnvironmentKey, notificationsAddressEnvironmentKey, notificationsPortEnvironmentKey, out notificationsConnectionString!))
                ThrowInvalidConfiguration(nameof(notificationsConnectionString));
            if (!BuildConnectionString(requestsProtocolEnvironmentKey, requestsAddressEnvironmentKey, requestsPortEnvironmentKey, out requestsConnectionString!))
                ThrowInvalidConfiguration(nameof(requestsConnectionString));
            if (!BuildConnectionString(responsesProtocolEnvironmentKey, responsesAddressEnvironmentKey, responsesPortEnvironmentKey, out responsesConnectionString!))
                ThrowInvalidConfiguration(nameof(responsesConnectionString));

            // Create communication workers
            signalsWorker = new SignalsWorker(signalsConnectionString);
            notificationsWorker = new NotificationsWorker(notificationsConnectionString);
            requestsWorker = new RequestsWorker(requestsConnectionString);
            responsesWorker = new ResponsesWorker(responsesConnectionString);

            requestsWorker.RequestReceived += OnRequestReceived;

            processId = Environment.ProcessId;
            promises = new();
        }

        private static HResult BuildConnectionString(
            string protocolKey, 
            string addressKey, 
            string portKey, 
            [NotNullWhen(returnValue: default)] out string? connectionString)
        {
            var protocol = Environment.GetEnvironmentVariable(protocolKey);
            var address = Environment.GetEnvironmentVariable(addressKey);
            var port = Environment.GetEnvironmentVariable(portKey);
            if (protocol is null || address is null || port is null)
            {
                Logger.LogError($"Could not create connection string, one or multiple parts are not defined: " +
                    $"{protocolKey}={protocol ?? "undefined"}, {addressKey}={address ?? "undefined"}, portKey={port ?? "undefined"}");
                connectionString = null;
                return HResult.E_FAIL;
            }

            connectionString = $"{protocol}://{address}:{port}";
            return HResult.S_OK;
        }

        [ThreadStatic]
        private static PushSocket? internalNotificationsSocket;

        public ulong SendNotification(NotifyMessage message)
        {
            if (internalNotificationsSocket is null)
            {
                internalNotificationsSocket = new PushSocket();
                internalNotificationsSocket.Connect(NotificationsWorker.InternalNotificationsConnectionString);
            }

            var notificationId = message.NotificationId = GetNewNotificationId();
            internalNotificationsSocket.SendFrame(message.ToByteArray());
            return notificationId;
        }

        [ThreadStatic]
        private static PushSocket? internalResponsesSocket;

        public void SendResponse(NotifyMessage message)
        {
            if (internalResponsesSocket is null)
            {
                internalResponsesSocket = new PushSocket();
                internalResponsesSocket.Connect(ResponsesWorker.InternalResponsesConnectionString);
            }

            internalResponsesSocket.SendFrame(message.ToByteArray());
        }

        public Task<RequestMessage> ReceiveRequest(ulong notificationId)
        {
            var tcs = new TaskCompletionSource<RequestMessage>();
            promises.AddOrUpdate(notificationId, tcs, (_, _) => tcs);
            return tcs.Task;
        }

        private void OnRequestReceived(RequestMessage request)
        {
            if (request.PayloadCase == RequestMessage.PayloadOneofCase.Ping)
            {
                // Send an immediate response to all pings
                var response = new NotifyMessage()
                {
                    Response = new Response()
                    {
                        RequestId = request.RequestId,
                        Result = true
                    },
                    ProcessId = processId
                };
                SendResponse(response);
            }
            else
            {
                // Non-trivial requests need to be processed first
                if (promises.TryRemove(request.NotificationId, out var tcs))
                {
                    tcs.SetResult(request);
                }
                else
                {
                    Logger.LogError("Communication protocol error: could not match request to an existing promise!");
                }
            }
        }

        public ulong GetNewNotificationId()
        {
            return Interlocked.Increment(ref notificationId);
        }

        public void Start()
        {
            signalsWorker.Start();
            Logger.LogDebug($"Signals worker connected: {signalsConnectionString}");

            notificationsWorker.Start();
            Logger.LogDebug($"Notifications worker connected: {notificationsConnectionString}");

            requestsWorker.Start();
            Logger.LogDebug($"Requests worker connected: {requestsConnectionString}");

            responsesWorker.Start();
            Logger.LogDebug($"Responses worker connected: {responsesConnectionString}");
        }

        public void Terminate()
        {
            notificationsWorker.Terminate();
            requestsWorker.Terminate();
            responsesWorker.Terminate();
            signalsWorker.Terminate();
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                notificationsWorker.Dispose();
                requestsWorker.Dispose();
                responsesWorker.Dispose();
                signalsWorker.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
}
