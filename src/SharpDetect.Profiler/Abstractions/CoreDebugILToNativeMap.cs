namespace SharpDetect.Profiler
{
    public readonly struct CorDebugILToNativeMap
    {
        public readonly uint IlOffset;
        public readonly uint NativeStartOffset;
        public readonly uint NativeEndOffset;
    }
}
