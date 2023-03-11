// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler.Logging;

internal class ConsoleSink : ILoggerSink
{
    public void Write(string message)
    {
        Console.Write(message);
    }

    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void Flush()
    {
        /* NOP */
    }

    public void Dispose()
    {
        /* NOP */
    }
}
