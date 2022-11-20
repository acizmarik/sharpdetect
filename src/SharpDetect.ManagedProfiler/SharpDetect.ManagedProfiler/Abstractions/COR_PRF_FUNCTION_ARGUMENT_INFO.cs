namespace SharpDetect.Profiler
{
    public readonly struct COR_PRF_FUNCTION_ARGUMENT_INFO
    {
        public readonly ulong NumRanges;
        public readonly ulong TotalArgumentSize;
        public readonly COR_PRF_FUNCTION_ARGUMENT_RANGE range1;
        public readonly COR_PRF_FUNCTION_ARGUMENT_RANGE range2;
    }
}
