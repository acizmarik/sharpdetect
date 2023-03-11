// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.VectorClock
{
    class LockState
    {
        public List<uint> Clock;
        public volatile bool Taken;

        private LockState(List<uint> clock)
        {
            Clock = clock;
        }

        public static LockState Create(List<uint> clock)
            => new(clock);
    }
}
