// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.VectorClock
{
    internal struct Epoch
    {
        public volatile uint Value;
        public const uint ReadSharedEpoch = 0xFF_FF_FF_FF;
        private const uint ThreadIdMark = 0xFF_00_00_00;
        private const uint ClockMask = 0x00_FF_FF_FF;
        private const int ThreadShift = 24;

        public int GetThreadId()
            => (int)((Value & ThreadIdMark) >> ThreadShift);

        public int GetClock()
            => (int)(Value & ClockMask);

        internal static void SetThreadId(ref Epoch epoch, int threadId)
        {
            epoch.Value &= ~ThreadIdMark;
            epoch.Value |= (uint)(threadId << ThreadShift);
        }

        internal static void SetClock(ref Epoch epoch, int clock)
        {
            epoch.Value &= ~ClockMask;
            epoch.Value |= (uint)(clock & ClockMask);
        }

        internal static void SetEpoch(ref Epoch epoch, uint newValue)
        {
            epoch.Value = newValue;
        }

        internal static void Increment(ref Epoch epoch)
        {
            epoch.Value++;
        }

        public override string ToString()
        {
            return $"EPOCH={Value};TID={GetThreadId()};CLK={GetClock()}";
        }
    }
}
