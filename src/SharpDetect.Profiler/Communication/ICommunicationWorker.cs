// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler.Communication;

internal interface ICommunicationWorker : IDisposable
{
    void Start();
    void Terminate();
}
