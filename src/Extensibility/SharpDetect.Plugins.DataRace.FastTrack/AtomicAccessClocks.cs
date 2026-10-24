// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class AtomicAccessClocks
{
    private AccessClock _accesses;

    public void Record(Epoch access, VectorClock accessorVc, ThreadClockTable threadClocks)
        => _accesses.Add(access, accessorVc, threadClocks);

    public ProcessThreadId? FindUnorderedAccess(ProcessThreadId threadId, VectorClock threadVc)
        => _accesses.FindUnordered(threadId, threadVc);

    private struct AccessClock
    {
        private Epoch _epoch;
        private VectorClock? _clock;

        public void Add(Epoch access, VectorClock accessorVc, ThreadClockTable threadClocks)
        {
            if (_clock != null)
            {
                _clock.SetClock(access.ThreadId, access.Clock);
            }
            else if (_epoch.IsNone || _epoch.ThreadId == access.ThreadId || _epoch.HappensBefore(accessorVc))
            {
                _epoch = access;
            }
            else
            {
                _clock = threadClocks.CreateClock();
                _clock.SetClock(_epoch.ThreadId, _epoch.Clock);
                _clock.SetClock(access.ThreadId, access.Clock);
            }
        }

        public readonly ProcessThreadId? FindUnordered(ProcessThreadId threadId, VectorClock threadVc)
        {
            if (_clock != null)
                return _clock.FindRacingReader(threadVc);

            return !_epoch.IsNone && _epoch.ThreadId != threadId && !_epoch.HappensBefore(threadVc)
                ? _epoch.ThreadId
                : null;
        }
    }
}
