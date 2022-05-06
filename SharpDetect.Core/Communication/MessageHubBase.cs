﻿using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Messages;

namespace SharpDetect.Core.Communication
{
    internal abstract class MessageHubBase
    {
        protected readonly HashSet<NotifyMessage.PayloadOneofCase> SupportedMessageTypes;
        protected readonly ILogger Logger;

        public MessageHubBase(ILogger logger, IEnumerable<NotifyMessage.PayloadOneofCase> types)
        {
            this.Logger = logger;
            SupportedMessageTypes = types.ToHashSet();
        }

        public bool CanHandle(NotifyMessage.PayloadOneofCase type)
        {
            return SupportedMessageTypes.Contains(type);
        }

        protected static EventInfo CreateEventInfo(NotifyMessage message)
            => new(message.NotificationId, message.ProcessId, new(message.ThreadId));
    }
}
