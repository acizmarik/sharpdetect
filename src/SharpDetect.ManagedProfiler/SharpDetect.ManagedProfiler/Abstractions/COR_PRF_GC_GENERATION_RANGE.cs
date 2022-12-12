namespace SharpDetect.Profiler
{
    public readonly struct COR_PRF_GC_GENERATION_RANGE
    {
        public readonly COR_PRF_GC_GENERATION Generation;
        public readonly ObjectId RangeStart;
        public readonly nuint RangeLength;
        public readonly nuint RangeLengthReserved;

        public COR_PRF_GC_GENERATION_RANGE(COR_PRF_GC_GENERATION generation, ObjectId start, nuint length, nuint lengthReserved)
        {
            Generation = generation;
            RangeStart = start;
            RangeLength = length;
            RangeLengthReserved = lengthReserved;
        }
    }
}
