// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler.Communication;

internal class NotificationsWorker : RelayWorkerBase
{
    public const string InternalNotificationsConnectionString = "inproc://profiling-notifications";

    public NotificationsWorker(string connectionString)
        : base(connectionString, InternalNotificationsConnectionString)
    {

    }
}
