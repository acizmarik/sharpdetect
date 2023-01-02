namespace SharpDetect.Profiler;

internal record AssemblyRefProps(
    MdAssemblyRef AssemblyRef,
    string Name,
    IntPtr PublicKey,
    ulong PublicKeyLength,
    ASSEMBLYMETADATA Metadata,
    DWORD Flags);
