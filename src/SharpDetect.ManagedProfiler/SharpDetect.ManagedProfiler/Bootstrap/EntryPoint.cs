using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

public static class EntryPoint
{
    private static ClassFactory classFactory = null!;

    [UnmanagedCallersOnly(EntryPoint = nameof(DllGetClassObject))]
    public static unsafe HResult DllGetClassObject([In] Guid* rclsid, [In] Guid* riid, [Out] IntPtr* ppv)
    {
        // Ensure that ppv is valid
        if (ppv == (void*)IntPtr.Zero)
            return HResult.E_FAIL;

        // Ensure that runtime actually requested us
        if (*rclsid != KnownGuids.SharpDetectProfiler)
            return HResult.E_FAIL;

        // Create an instance of profiler factory
        classFactory = new ClassFactory();
        *ppv = classFactory.Object;

        return HResult.S_OK;
    }
}
