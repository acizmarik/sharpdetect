// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Messages;
using NetMQ.Sockets;
using Google.Protobuf;
using NetMQ;

namespace SharpDetect.Profiler.Communication;

internal class SignalsWorker : ICommunicationWorker
{
    private readonly string connectionString;
    private readonly PushSocket socket;
    private readonly Thread thread;
    private readonly ManualResetEvent termination;
    private bool isDisposed;

    public SignalsWorker(string connectionString)
    {
        this.connectionString = connectionString;
        this.socket = new PushSocket();
        this.thread = new Thread(ThreadLoop);
        this.termination = new(initialState: false);
    }

    private void ThreadLoop()
    {
        // Prepare static array of message bytes since it does not change
        var message = new NotifyMessage()
        {
            Heartbeat = new Notify_Heartbeat(), 
            ProcessId = Environment.ProcessId 
        };
        var messageBytes = message.ToByteArray();

        while (true)
        {
            // Send heartbeat
            socket.SendFrame(messageBytes);

            // Wait before sending next heartbeat or until we are terminated
            if (termination.WaitOne(TimeSpan.FromSeconds(25)))
                break;
        }
    }

    public void Start()
    {
        socket.Connect(connectionString);
        thread.Start();
    }

    public void Terminate()
    {
        termination.Set();
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            Terminate();
            if (thread.ThreadState != ThreadState.Unstarted)
                thread.Join();
            socket.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
