// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Messages;

namespace SharpDetect.Common.Services.Endpoints
{
    public interface IRequestsProducer : IEndpoint
    {
        Task<Response> SendAsync(int processId, RequestMessage message);
    }
}
