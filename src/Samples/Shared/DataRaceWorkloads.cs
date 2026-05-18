// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace Shared;

public static class DataRaceWorkloads
{
    public static int Field;

    public static void RaceFact()
    {
        Field = 0;
        RunConcurrently(
            () => Field = 1,
            () => Field = 2);
    }

    public static void CleanFact()
    {
        Field = 0;
        var t1 = new Thread(() => Field = 1);
        t1.Start();
        t1.Join();
        var t2 = new Thread(() => Field = 2);
        t2.Start();
        t2.Join();
    }

    private static void RunConcurrently(params Action[] actions)
    {
        using var start = new ManualResetEventSlim(initialState: false);
        using var ready = new CountdownEvent(initialCount: actions.Length);
        var threads = actions
            .Select(action => new Thread(() =>
            {
                ready.Signal();
                start.Wait();
                action();
            }))
            .ToArray();

        foreach (var thread in threads)
            thread.Start();
        ready.Wait();
        start.Set();
        foreach (var thread in threads)
            thread.Join();
    }
}
