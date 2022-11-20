namespace SharpDetect.Profiler
{
    public readonly struct COR_PRF_GC_GENERATION_RANGE
    {
        public readonly COR_PRF_GC_GENERATION generation;
        public readonly ObjectId RangeStart;
        public readonly nuint RangeLength;
        public readonly nuint RangeLengthReserved;
    }
}
