namespace SharpDetect.Profiler.Hooks.PAL.Windows
{
    [Flags]
    internal enum MemoryProtection : uint
    {
        EXECUTE = 0x10,
        EXECUTE_READ = 0x20,
        EXECUTE_READWRITE = 0x40,
        EXECUTE_WRITECOPY = 0x80,
        NOACCESS = 0x01,
        READONLY = 0x02,
        READWRITE = 0x04,
        WRITECOPY = 0x08,
        GUARD = 0x100,
        NOCACHE = 0x200,
        WRITECOMBINE = 0x400
    }
}
