// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Profiler;

internal unsafe class Assembly
{
    public readonly AssemblyId AssemblyId;
    public ISet<AssemblyRefProps> References => assemblyReferences;

    private readonly IMetaDataAssemblyImport metadataAssemblyImport;
    private readonly IMetaDataAssemblyEmit metadataAssemblyEmit;
    private readonly HashSet<AssemblyRefProps> assemblyReferences;
    private const int maxNameCharactersCount = 1000;

    public Assembly(ModuleId moduleId, ICorProfilerInfo3 profilerInfo)
    {
        assemblyReferences = new HashSet<AssemblyRefProps>();

        // Obtain IMetaDataAssemblyImport
        var imetadataAssemblyImportGuid = KnownGuids.IMetaDataAssemblyImport;
        if (!profilerInfo.GetModuleMetaData(moduleId, CorOpenFlags.ofRead, &imetadataAssemblyImportGuid, out var ppOut))
            throw new ArgumentException($"Could not obtain {nameof(KnownGuids.IMetaDataAssemblyImport)}");
        metadataAssemblyImport = NativeObjects.IMetaDataAssemblyImport.Wrap(ppOut);

        // Obtain IMetaDataAssemblyEmit
        var imetadataAssemblyEmitGuid = KnownGuids.IMetaDataAssemblyEmit;
        if (!profilerInfo.GetModuleMetaData(moduleId, CorOpenFlags.ofRead | CorOpenFlags.ofWrite, &imetadataAssemblyEmitGuid, out ppOut))
            throw new ArgumentException($"Could not obtain {KnownGuids.IMetaDataAssemblyEmit}");
        metadataAssemblyEmit = NativeObjects.IMetaDataAssemblyEmit.Wrap(ppOut);

        // Load assembly references
        if (!LoadReferences())
            throw new ArgumentException("Could not load assembly references");

        // Obtain AssemblyId
        if (!profilerInfo.GetModuleInfo2(moduleId, out _, 0, out _, null, out var assemblyId, out _))
            throw new ArgumentException("Could not obtain AssemblyId");

        this.AssemblyId = assemblyId;
    }

    /// <summary>
    /// Gets assembly metadata
    /// </summary>
    /// <param name="name">Assembly name</param>
    /// <param name="publicKey">Pointer to the public key</param>
    /// <param name="cbData">Length of public key data in bytes</param>
    /// <param name="metadata">Assembly metadata</param>
    /// <param name="flags">Assembly flags</param>
    public HResult GetAssemblyProps(
        [NotNullWhen(returnValue: default)] out string? name, 
        out IntPtr publicKey, 
        out ulong cbData, 
        out ASSEMBLYMETADATA metadata, 
        out DWORD flags)
    {
        name = null;
        metadata = default;
        publicKey = IntPtr.Zero;
        cbData = 0;
        flags = 0;

        if (!metadataAssemblyImport.GetAssemblyFromScope(out var mdAssembly))
            return HResult.E_FAIL;

        Span<char> nameBuffer = stackalloc char[maxNameCharactersCount];
        fixed (char* namePtr = nameBuffer)
        {
            if (!metadataAssemblyImport.GetAssemblyProps(mdAssembly, out publicKey, out cbData, out _,
                namePtr, maxNameCharactersCount, out var nameLength, out metadata, out flags))
            {
                return HResult.E_FAIL;
            }

            name = new string(namePtr, 0, (int)nameLength - 1);
            return HResult.S_OK;
        }
    }

    /// <summary>
    /// Creates or retrieves an existing assembly reference
    /// </summary>
    /// <param name="assemblyName">Referenced name of assembly</param>
    /// <param name="publicKey">Pointer to the referenced assembly's public key</param>
    /// <param name="cbPublicKey">Length of public key data in bytes</param>
    /// <param name="metadata">Assembly metadata</param>
    /// <param name="flags">Assembly flags</param>
    /// <param name="assemblyRef">Assembly reference</param>
    public HResult AddOrGetAssemblyRef(
        string assemblyName, 
        IntPtr publicKey, 
        ulong cbPublicKey, 
        ASSEMBLYMETADATA metadata, 
        DWORD flags, 
        out MdAssemblyRef assemblyRef)
    {
        // Check if the reference already exists
        var reference = assemblyReferences.FirstOrDefault(
            r => r.Name == assemblyName &&
                 r.Flags == flags &&
                 ArePublicKeysEqual(publicKey, cbPublicKey, r) &&
                 AreMetadataEqual(in metadata, r));
        if (reference is not null)
        {
            // Assembly already contains this reference
            assemblyRef = reference.AssemblyRef;
            return HResult.S_OK;
        }

        // Otherwise create a new assembly reference
        fixed (char* namePtr = assemblyName)
        {
            // Create assembly reference if does not exist
            if (!metadataAssemblyEmit.DefineAssemblyRef(publicKey, cbPublicKey, namePtr, in metadata,
                IntPtr.Zero, 0, flags, out assemblyRef))
            {
                return HResult.E_FAIL;
            }

            // Cache assembly reference
            assemblyReferences.Add(new(
                assemblyRef, 
                assemblyName, 
                publicKey, 
                cbPublicKey, 
                metadata, 
                flags));
        }

        return HResult.S_OK;
    }

    private static bool ArePublicKeysEqual(IntPtr publicKey1, ulong publicKey1Length, AssemblyRefProps other)
    {
        // Different lengths
        if (publicKey1Length != other.PublicKeyLength)
            return false;

        // Invalid pointers
        if ((publicKey1Length > 0 && publicKey1 == IntPtr.Zero) ||
            (other.PublicKeyLength > 0 && other.PublicKey == IntPtr.Zero))
            return false;

        // Check byte by byte
        var publicKey1Ptr = (byte*)publicKey1.ToPointer();
        var publicKey2Ptr = (byte*)other.PublicKey.ToPointer();
        for (var i = 0; i < (int)publicKey1Length; i++)
        {
            if (*publicKey1Ptr++ != *publicKey2Ptr++)
                return false;
        }

        return true;
    }

    private static bool AreMetadataEqual(in ASSEMBLYMETADATA metadata1, AssemblyRefProps other)
    {
        // FIXME: we should also check other metadata properties
        if (metadata1.usMajorVersion != other.Metadata.usMajorVersion ||
            metadata1.usMinorVersion != other.Metadata.usMinorVersion ||
            metadata1.usRevisionNumber != other.Metadata.usRevisionNumber ||
            metadata1.usBuildNumber != other.Metadata.usBuildNumber)
            return false;

        return true;
    }

    /// <summary>
    /// Load all assembly references. The result is cached
    /// </summary>
    private HResult LoadReferences()
    {
        HResult hr;
        IntPtr enumerator;
        Span<char> nameBuffer = stackalloc char[maxNameCharactersCount];
        
        do
        {
            hr = metadataAssemblyImport.EnumAssemblyRefs(&enumerator, out var currentAssemblyRef, 1, out var count);
            if (hr != HResult.S_OK)
                break;

            fixed (char* namePtr = nameBuffer)
            {
                hr = metadataAssemblyImport.GetAssemblyRefProps(
                    currentAssemblyRef, 
                    out IntPtr publicKeyData, 
                    out ulong cbPublicKey, 
                    namePtr, 
                    maxNameCharactersCount,
                    out var nameLength, 
                    out var metadata, 
                    out _ /* hash ptr */, 
                    out _ /* hash byte length */, 
                    out var flags);
                var name = new string(namePtr, 0, (int)nameLength - 1);
                assemblyReferences.Add(new(currentAssemblyRef, name, publicKeyData, cbPublicKey, metadata, flags));
            }

        } while (hr == HResult.S_OK);

        metadataAssemblyImport.CloseEnum(enumerator);
        return HResult.S_OK;
    }
}
