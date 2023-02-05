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
                CurrentEpoch = new(CurrentEpoch.Value + 1);
        }

        public void WaitForChange(ShadowThread thread)
        {
            lock (lockObj)
            {
                while (CurrentEpoch.Value < thread.Epoch.Value)
                    Monitor.Wait(lockObj);
            }
        }
    }
}
