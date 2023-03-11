// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using NetMQ;

namespace SharpDetect.Profiler.Communication;

internal class ResponsesWorker : RelayWorkerBase
{
    public const string InternalResponsesConnectionString = "inproc://profiling-responses";

    public ResponsesWorker(string connectionString)
        : base(connectionString, InternalResponsesConnectionString)
    {

    }

    protected override void OnNotificationReady(object? _, NetMQSocketEventArgs e)
    {
        base.OnNotificationReady(_, e);
    }
}
