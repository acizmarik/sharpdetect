namespace SharpDetect.Plugins.VectorClock
{
    internal class ThreadState
    {
        public readonly int ThreadId;
        internal List<uint> Clock;
        internal Epoch Epoch;

        public ThreadState(int threadId)
        {
            Clock = new List<uint>(Enumerable.Repeat<uint>(0, threadId + 1));
            ThreadId = threadId;
            Epoch.SetEpoch(ref Epoch, ((uint)threadId << 24));
        }

        public void UpdateEpoch()
        {
            Epoch.SetEpoch(ref Epoch, Clock[ThreadId]);
        }

        public void IncrementEpoch()
        {
            Epoch.Increment(ref Epoch);
            Clock[ThreadId] = Epoch.Value;
        }

        public override string ToString()
        {
            var state = (Epoch.Value == Clock[ThreadId]) ? "OK" : "INVALID";
            return $"Thread: [TID={ThreadId}; {Epoch}; STATE={state}]";
        }
    }
}
