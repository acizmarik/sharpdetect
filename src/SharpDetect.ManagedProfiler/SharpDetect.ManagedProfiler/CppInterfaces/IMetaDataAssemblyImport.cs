using System.Runtime.InteropServices;

namespace SharpDetect.Profiler
{
    [NativeObject]
    internal unsafe interface IMetaDataAssemblyImport : IUnknown
    {
        HResult GetAssemblyProps(
            [In] MdAssembly mda,
            [Out] out IntPtr ppbPublicKey,
            [Out] out ulong pcbPublicKey,
            [Out] out ulong pulHashAlgId,
            [Out] char* szName,
            [In] ulong cchName,
            [Out] out ulong pchName,
            [Out] out ASSEMBLYMETADATA pMetaData,
            [Out] out uint pdwAssemblyFlags);

        HResult GetAssemblyRefProps(
            [In] MdAssemblyRef mdar,
            [Out] out IntPtr ppbPublicKeyOrToken,
            [Out] out ulong pcbPublicKeyOrToken,
            [Out] char* szName,
            [In] ulong cchName,
            [Out] out ulong pchName,
            [Out] out ASSEMBLYMETADATA pMetaData,
            [Out] out IntPtr ppbHashValue,
            [Out] out ulong pcbHashValue,
            [Out] out uint pdwAssemblyRefFlags);

        HResult GetFileProps(
            [In] MdFile mdf,
            [Out] char* szName,
            [In] ulong cchName,
            [Out] out ulong pchName,
            [Out] out IntPtr ppbHashValue,
            [Out] out ulong pcbHashValue,
            [Out] out uint pdwFileFlags);

        HResult GetExportedTypeProps(
            [In] MdExportedType mdct,
            [Out] char* szName,
            [In] ulong cchName,
            [Out] ulong* pchName,
            [Out] MdToken* ptkImplementation,
            [Out] MdTypeDef* ptkTypeDef,
            [Out] uint* pdwExportedTypeFlags);

        HResult GetManifestResourceProps(
            [In] MdManifestResource mdmr,
            [Out] char* szName,
            [In] ulong cchName,
            [Out] out ulong pchName,
            [Out] out MdToken ptkImplementation,
            [Out] out uint pdwOffset,
            [Out] out uint pdwResourceFlags);

        HResult EnumAssemblyRefs(
            [In, Out] HCORENUM* phEnum,
            [Out] out MdAssemblyRef rAssemblyRefs,
            [In] ulong cMax,
            [Out] out ulong pcTokens);

        HResult EnumFiles(
            [In, Out] HCORENUM* phEnum,
            [Out] out MdFile rFiles,
            [In] ulong cMax,
            [Out] out ulong pcTokens);

        HResult EnumExportedTypes(
            [In, Out] HCORENUM* phEnum,
            [Out] out MdExportedType rExportedTypes,
            [In] ulong cMax,
            [Out] out ulong pcTokens);

        HResult EnumManifestResources(
            [In, Out] HCORENUM* phEnum,
            [Out] out MdManifestResource rManifestResources,
            [In] ulong cMax,
            [Out] out ulong pcTokens);

        HResult GetAssemblyFromScope(
            [Out] out MdAssembly ptkAssembly);

        HResult FindExportedTypeByName(
            [In] char* szName,
            [In] MdToken mdtExportedType,
            [Out] out MdExportedType ptkExportedType);

        HResult FindManifestResourceByName(
            [In] char* szName,
            [Out] out MdManifestResource ptkManifestResource);

        void CloseEnum(
            [In] HCORENUM hEnum);

        HResult FindAssembliesByName(
            [In] char* szAppBase,
            [In] char* szPrivateBin,
            [In] char* szAssemblyName,
            [Out] out IntPtr ppIUnk,
            [In] ulong cMax,
            [Out] out ulong pcAssemblies);
    }
}
