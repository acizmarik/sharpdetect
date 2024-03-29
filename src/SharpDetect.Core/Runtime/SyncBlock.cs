﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CommunityToolkit.Diagnostics;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;

namespace SharpDetect.Core.Runtime
{
    internal class SyncBlock : ISyncBlock
    {
        public UIntPtr? LockOwnerId => LockOwner?.Id;
        public IShadowThread? LockOwner => lockOwner;

        private volatile IShadowThread? lockOwner;
        private int reentrancyCounter;

        public SyncBlock()
        {
            reentrancyCounter = 0;
            lockOwner = null;
        }

        public void Acquire(IShadowThread thread)
        {
            lock (this)
            {
                for (;;)
                {
                    if (lockOwner == null)
                    {
                        // Lock acquire
                        reentrancyCounter = 1;
                        lockOwner = thread;
                        return;
                    }
                    else if (lockOwner == thread)
                    {
                        // Reentrancy
                        reentrancyCounter++;
                        return;
                    }
                    else
                    {
                        // Wait for release signal
                        Monitor.Wait(this);
                    }
                }
            }
        }

        public void Release(IShadowThread thread)
        {
            lock (this)
            {
                RuntimeContract.Assert(lockOwner != null);
                RuntimeContract.Assert(lockOwner == thread);

                // Check if the lock was locked multiple times
                if (--reentrancyCounter != 0)
                    return;

                lockOwner = null;
                Monitor.Pulse(this);
            }
        }
    }
}
