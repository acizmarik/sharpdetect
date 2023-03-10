using SharpDetect.Profiler.Logging;
using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

public static class EntryPoint
{
    private static ClassFactory classFactory = null!;
    private const DWORD DLL_PROCESS_DETACH = 0;
    private const DWORD DLL_PROCESS_ATTACH = 1;
    private const DWORD DLL_THREAD_ATTACH = 2;
    private const DWORD DLL_THREAD_DETACH = 3;

    [UnmanagedCallersOnly(EntryPoint = nameof(DllMain))]
    public static unsafe bool DllMain(IntPtr hInstDll, DWORD reason, IntPtr _)
    {
        if (reason == DLL_PROCESS_DETACH)
        {
            // Ensure shutdown is always called
            CorProfilerCallback.Instance?.Shutdown();
        }

        return true;
    }


    [UnmanagedCallersOnly(EntryPoint = nameof(DllGetClassObject))]
    public static unsafe HResult DllGetClassObject([In] Guid* rclsid, [In] Guid* riid, [Out] IntPtr* ppv)
    {
        Logger.Initialize(
            LogLevel.Debug,
            new ConsoleSink(),
            new FileSink("profiler-log.txt", append: false));
        Logger.LogDebug(nameof(DllGetClassObject));

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