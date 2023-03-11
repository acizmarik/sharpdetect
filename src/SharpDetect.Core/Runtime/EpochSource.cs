// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Runtime.Threads;

namespace SharpDetect.Core.Runtime
{
    internal readonly record struct Epoch(ulong Value);

    internal class EpochSource
    {
        public Epoch CurrentEpoch { get; private set; }
        private readonly object lockObj = new();

        public void Increment()
        {
            lock (lockObj)
            {
                CurrentEpoch = new(CurrentEpoch.Value + 1);
                Monitor.PulseAll(lockObj);
            }
        }

        public void WaitForNextEpoch(ShadowThread thread)
        {
            lock (lockObj)
            {
                if (CurrentEpoch.Value < thread.Epoch.Value)
                    Monitor.Wait(lockObj);
            }
        }
    }
}
