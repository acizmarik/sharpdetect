// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Messages;

namespace SharpDetect.Core.Communication
{
    internal interface INotificationsHandler
    {
        bool CanHandle(NotifyMessage.PayloadOneofCase type);

        void Process(NotifyMessage message);
    }
}
