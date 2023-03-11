// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler.Logging;

internal interface ILoggerSink : IDisposable
{
    void Write(string message);
    void WriteLine(string message);
    void Flush();
}
