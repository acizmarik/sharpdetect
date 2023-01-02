using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

[NativeObject]
internal unsafe interface IMethodAlloc : IUnknown
{
    public IntPtr Alloc(
        [In] ulong cb);
}
