// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

[NativeObject]
internal unsafe interface IMetaDataAssemblyEmit : IUnknown
{
    public HResult DefineAssembly(
        [In] IntPtr pbPublicKey,
        [In] ulong cbPublicKey,
        [In] ulong uHashAlgId,
        [In] char* szName,
        [In] in ASSEMBLYMETADATA pMetaData,
        [In] DWORD dwAssemblyFlags,
        [Out] out MdAssembly pmda);

    public HResult DefineAssemblyRef(
        [In] IntPtr pbPublicKeyOrToken,
        [In] ulong cbPublicKeyOrToken,
        [In] char* szName,
        [In] in ASSEMBLYMETADATA pMetaData,
        [In] IntPtr pbHashValue,
        [In] ulong cbHashValue,
        [In] DWORD dwAssemblyRefFlags,
        [Out] out MdAssemblyRef pmdar);

    public HResult DefineFile(
        [In] char* szName,
        [In] IntPtr pbHashValue,
        [In] ulong cbHashValue,
        [In] DWORD dwFileFlags,
        [Out] out MdFile pmdf);

    public HResult DefineExportedType(
        [In] char* szName,
        [In] MdToken tkImplementation,
        [In] MdTypeDef tkTypeDef,
        [In] DWORD dwExportedTypeFlags,
        [Out] out MdExportedType pmdct);

    public HResult DefineManifestResource(
        [In] char* szName,
        [In] MdToken tkImplementation,
        [In] DWORD dwOffset,
        [In] DWORD dwResourceFlags,
        [Out] out MdManifestResource pmdmr);

    public HResult SetAssemblyProps(
        [In] MdAssembly pma,
        [In] IntPtr pbPublicKey,
        [In] ulong cbPublicKey,
        [In] ulong ulHashAlgId,
        [In] char* szName,
        [In] in ASSEMBLYMETADATA pMetaData,
        [In] DWORD dwAssemblyFlags);

    public HResult SetAssemblyRefProps(
        [In] MdAssemblyRef ar,
        [In] IntPtr pbPublicKeyOrToken,
        [In] ulong cbPublicKeyOrToken,
        [In] char* szName,
        [In] in ASSEMBLYMETADATA pMetaData,
        [In] IntPtr pbHashValue,
        [In] ulong cbHashValue,
        [In] DWORD dwAssemblyRefFlags);

    public HResult SetFileProps(
        [In] MdFile file,
        [In] IntPtr pbHashValue,
        [In] ulong cbHashValue,
        [In] DWORD dwFileFlags);

    public HResult SetExportedTypeProps(
        [In] MdExportedType ct,
        [In] MdToken tkImplementation,
        [In] MdTypeDef tkTypeDef,
        [In] DWORD dwExportedTypeFlags);

    public HResult SetManifestResourceProps(
        [In] MdManifestResource mr,
        [In] MdToken tkImplementation,
        [In] DWORD dwOffset,
        [In] DWORD dwResourceFlags);
}
