// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

var lockObj1 = new object();
var lockObj2 = new object();

Console.WriteLine("Hello World!");

var thread1 = new Thread(() =>
{
    for (; ; )
    {
        Console.WriteLine("Hello 1:0");
        lock (lockObj1)
        {
            Console.WriteLine("Hello 1:1");
            lock (lockObj2)
            {
                Console.WriteLine("Hello 1:2");
            }
        }
    }
});

var thread2 = new Thread(() =>
{
    for (; ; )
    {
        Console.WriteLine("Hello 2:0");
        lock (lockObj2)
        {
            Console.WriteLine("Hello 2:1");
            lock (lockObj1)
            {
                Console.WriteLine("Hello 2:2");
            }
        }
    }
});

thread1.Name = "Subject 1";
thread1.IsBackground = true;
thread2.Name = "Subject 2";
thread2.IsBackground = true;
thread1.Start();
thread2.Start();

Thread.Sleep(2000);
