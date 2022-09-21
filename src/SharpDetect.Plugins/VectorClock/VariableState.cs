namespace SharpDetect.Plugins.VectorClock
{
    internal class VariableState
    {
        internal Epoch Read;
        internal Epoch Write;
        internal List<uint>? Clock;

        public static VariableState InitFromRead(ThreadState thread)
            => new(thread, false);

        public static VariableState InitFromWrite(ThreadState thread)
            => new(thread, true);

        private VariableState(ThreadState thread, bool isWrite)
        {
            if (isWrite)
                Write = thread.Epoch;
            else
                Read = thread.Epoch;
        }

        public override string ToString()
        {
            return $"Variable: [Read={Read}; Write={Write}]";
        }
    }
}
