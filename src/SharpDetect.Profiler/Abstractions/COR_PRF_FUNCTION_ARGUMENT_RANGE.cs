using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

[StructLayout(LayoutKind.Sequential)]
public readonly struct COR_PRF_FUNCTION_ARGUMENT_RANGE
{
    public readonly nint StartAddress;
    public readonly ulong Length;
}
