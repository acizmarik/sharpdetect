using System.Runtime.InteropServices;

namespace SharpDetect.Profiler.Hooks.PAL.Windows
{
    internal static partial class WindowsNativeFunctions
    {
        private const string Library = "kernel32.dll";

        [LibraryImport(Library)]
        public static partial IntPtr VirtualAlloc(
            IntPtr lpAddress,
            DWORD dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        [LibraryImport(Library)]
        [return:MarshalAs(UnmanagedType.I1)]
        public static partial bool VirtualFree(
            IntPtr lpAddress,
            DWORD dwSize,
            FreeType dwFreeType);
    }
}
