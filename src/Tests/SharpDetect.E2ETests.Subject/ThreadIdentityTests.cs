// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.E2ETests.Subject;

public static partial class Program
{
    // Keep in sync with ThreadIdentityTests.RacyIterations in SharpDetect.E2ETests
    private const int ThreadIdentityRacyIterations = 40;
    // Keep in sync with ThreadIdentityTests.SequentialThreads in SharpDetect.E2ETests
    private const int ThreadIdentitySequentialThreads = 300;

    private sealed class ThreadIdentityBox
    {
        public int Value;
    }
    
    public static void Test_DataRace_ThreadIdentity_GarbageCollectionBetweenRacingWrites()
    {
        for (var i = 0; i < ThreadIdentityRacyIterations; i++)
        {
            var box = new ThreadIdentityBox();
            using var release = new ManualResetEventSlim(initialState: false);
            var a = new Thread(() =>
            {
                box.Value = 1;
                release.Wait();
            });
            a.Start();
            Thread.Sleep(20);
            GC.Collect();
            var b = new Thread(() => box.Value = 2);
            b.Start();
            b.Join();
            release.Set();
            a.Join();
        }
    }

    public static void Test_DataRace_ThreadIdentity_GarbageCollectionBetweenRacingWrites_NoThreadExits()
    {
        using var end = new ManualResetEventSlim(initialState: false);
        var threads = new List<Thread>();
        for (var i = 0; i < ThreadIdentityRacyIterations; i++)
        {
            var box = new ThreadIdentityBox();
            var a = new Thread(() =>
            {
                box.Value = 1;
                end.Wait();
            });
            a.Start();
            threads.Add(a);
            Thread.Sleep(20);
            GC.Collect();
            var b = new Thread(() =>
            {
                box.Value = 2;
                end.Wait();
            });
            b.Start();
            threads.Add(b);
            Thread.Sleep(5);
        }

        end.Set();
        foreach (var thread in threads)
            thread.Join();
    }
    
    public static void Test_DataRace_ThreadIdentity_ExitedThreadNotJoined()
    {
        for (var i = 0; i < ThreadIdentityRacyIterations; i++)
        {
            var box = new ThreadIdentityBox();
            var a = new Thread(() => box.Value = 1);
            a.Start();
            // Thread.IsAlive is not a synchronization operation, unlike Thread.Join
            while (a.IsAlive)
                Thread.Sleep(1);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var b = new Thread(() => box.Value = 2);
            b.Start();
            b.Join();
        }
    }

    public static void Test_ThreadIdentity_SequentialThreadsWithGarbageCollections()
    {
        for (var i = 0; i < ThreadIdentitySequentialThreads; i++)
        {
            var thread = new Thread(static () => { });
            thread.Start();
            thread.Join();
            if (i % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
