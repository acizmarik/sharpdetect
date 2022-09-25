using SharpDetect.Common.Plugins.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Plugins.VectorClock
{
    [PluginExport("FastTrack", "1.0.0")]
    [PluginDiagnosticsCategories(new string[] { DiagnosticsCategory })]
    public partial class FastTrackPlugin : NopPlugin
    {
        public const string DiagnosticsCategory = "Data-race";
        public const string DiagnosticsMessageFormatArrays = "Possible data-race on an array element: {arrayInstance}[{index}]";
        public const string DiagnosticsMessageFormatFields = "Possible data-race on a field: {field}";

        private static bool Read(VariableState variable, ThreadState thread)
        {
            bool valid = true;

            // Same epoch 63.4%
            if (variable.Read.Value == thread.Epoch.Value)
                return true;

            // Check for write-read data-race
            if (variable.Write.Value > thread.Clock[variable.Write.GetThreadId()])
                valid = false;

            // Shared 20.8%
            if (variable.Read.Value == Epoch.ReadSharedEpoch)
            {
                ExtendVectorClockTo(ref variable.Clock, thread.Clock.Count);
                variable.Clock[thread.ThreadId] = thread.Epoch.Value;
            }
            else
            {
                // Exclusive access 15.7%
                if (variable.Read.Value <= thread.Clock[variable.Read.GetThreadId()])
                    variable.Read = thread.Epoch;
                else
                {
                    // Share 0.1%
                    if (variable.Clock == null)
                    {
                        variable.Clock = new List<uint>();
                        NewVectorClock(variable.Clock, thread.Clock.Count + 1);
                    }

                    variable.Clock[variable.Read.GetThreadId()] = variable.Read.Value;
                    variable.Clock[thread.ThreadId] = thread.Epoch.Value;
                    Epoch.SetEpoch(ref variable.Read, Epoch.ReadSharedEpoch);
                }
            }

            return valid;
        }

        private bool Write(VariableState variable, ThreadState thread)
        {
            bool valid = true;

            // Same epoch 71.0%
            if (variable.Write.Value == thread.Epoch.Value)
                return true;

            // Check for write-write data-race
            if (variable.Write.Value > thread.Clock[variable.Write.GetThreadId()])
                valid = false;

            // Check for read-write data-race
            if (variable.Read.Value != Epoch.ReadSharedEpoch)
            {
                // Shared 28.9%
                if (variable.Read.Value > thread.Clock[variable.Read.GetThreadId()])
                    valid = false;
            }
            else
            {
                // Exclusive 0.1%
                for (var i = 0; i < Math.Min(variable.Clock!.Count, lastThreadId + 1); i++)
                {
                    if (variable.Clock[i] > thread.Clock[i])
                        valid = false;
                }
            }

            Epoch.SetEpoch(ref variable.Write, thread.Epoch.Value);
            return valid;
        }

        private void Fork(ThreadState original, ThreadState forked)
        {
            Interlocked.Increment(ref threadsCount);
            lastThreadId = forked.ThreadId;
            var clockSize = lastThreadId + 1;

            ExtendVectorClockTo(ref original.Clock, lastThreadId + 1);
            ExtendVectorClockTo(ref forked.Clock, lastThreadId + 1);

            for (var i = 0; i < forked.Clock.Count; i++)
                forked.Clock[i] = Math.Max(original.Clock[i], forked.Clock[i]);
            foreach (var thread in threads.Values)
            {
                for (var i = thread.Clock.Count; i < clockSize; i++)
                {
                    var epoch = (uint)i << 24;
                    if (thread.ThreadId == i)
                        epoch++;

                    thread.Clock.Add(epoch);
                }
            }

            forked.UpdateEpoch();
            original.IncrementEpoch();
        }

        private void Join(ThreadState original, ThreadState joined)
        {
            Interlocked.Decrement(ref threadsCount);

            var clockSize = Math.Max(original.Clock.Count, joined.Clock.Count);
            ExtendVectorClockTo(ref original.Clock, clockSize);
            ExtendVectorClockTo(ref joined.Clock, clockSize);

            for (var i = 0; i < joined.Clock.Count; i++)
                original.Clock[i] = Math.Max(original.Clock[i], joined.Clock[i]);

            original.UpdateEpoch();
            joined.IncrementEpoch();
        }

        private static void Acquire(ThreadState thread, LockState @lock)
        {
            var clockSize = Math.Max(thread.Clock.Count, @lock.Clock.Count);
            ExtendVectorClockTo(ref thread.Clock, clockSize);
            ExtendVectorClockTo(ref @lock.Clock, clockSize);

            for (var i = 0; i < @lock.Clock.Count; ++i)
                thread.Clock[i] = Math.Max(thread.Clock[i], @lock.Clock[i]);

            thread.UpdateEpoch();
            @lock.Taken = true;
        }

        private static void Release(ThreadState thread, LockState @lock)
        {
            var clockSize = Math.Max(thread.Clock.Count, @lock.Clock.Count);
            ExtendVectorClockTo(ref thread.Clock, clockSize);
            ExtendVectorClockTo(ref @lock.Clock, clockSize);

            for (var i = 0; i < @lock.Clock.Count; i++)
                @lock.Clock[i] = thread.Clock[i];

            thread.IncrementEpoch();
            @lock.Taken = false;
        }

        private static void NewVectorClock(List<uint> clock, int size)
        {
            clock.Capacity = size;
            for (uint i = 0; i < size; i++)
                clock.Add(i << 24);
        }

        private static void ExtendVectorClockTo([NotNull] ref List<uint>? clock, int size)
        {
            clock ??= new List<uint>(size);
            var extendBy = size - clock.Count;
            if (extendBy <= 0)
                return;

            for (int i = clock.Count; i < size; i++)
                clock.Add((uint)i << 24);
        }
    }
}
