namespace SharpDetect.Profiler.Hooks.PAL.Windows;

[Flags]
internal enum AllocationType : uint
{
    COMMIT = 0x1000,
    RESERVE = 0x2000,
    RESET = 0x80000,
    LARGE_PAGES = 0x20000000,
    PHYSICAL = 0x400000,
    TOP_DOWN = 0x100000,
    WRITE_WATCH = 0x200000
}
