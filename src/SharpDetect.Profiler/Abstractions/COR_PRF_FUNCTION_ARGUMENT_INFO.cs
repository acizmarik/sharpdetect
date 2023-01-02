using System.Runtime.InteropServices;

namespace SharpDetect.Profiler
{
    public readonly struct COR_PRF_FUNCTION_ARGUMENT_INFO
    {
        private const int NumRangesOffset = 0;
        private const int TotalArgumentSizeOffset = 4;
        private const int RangesOffset = 8;
        private readonly int RangeSize = IntPtr.Size + sizeof(ulong);

        public uint NumRanges => MemoryMarshal.Read<uint>(new(data, NumRangesOffset, data.Length - NumRangesOffset));
        public uint TotalArgumentSize => MemoryMarshal.Read<uint>(new(data, TotalArgumentSizeOffset, data.Length - TotalArgumentSizeOffset));
        private readonly byte[] data;

        public COR_PRF_FUNCTION_ARGUMENT_INFO(byte[] data)
        {
            this.data = data;
        }

        public COR_PRF_FUNCTION_ARGUMENT_RANGE GetRange(int index)
        {
            var start = RangesOffset + (RangeSize * index);
            return MemoryMarshal.Read<COR_PRF_FUNCTION_ARGUMENT_RANGE>(new ReadOnlySpan<byte>(data, start, data.Length - start));
        }
    }
}
