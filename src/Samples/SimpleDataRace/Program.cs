// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

Test.Field = 0;

var thread1 = new Thread(() =>
{
    Test.Field = 1;
});

var thread2 = new Thread(() =>
{
    Test.Field = 2;
});

thread1.Start();
thread2.Start();
thread1.Join();
thread2.Join();

Console.WriteLine(Test.Field);

static class Test
{
    public static int Field;
}