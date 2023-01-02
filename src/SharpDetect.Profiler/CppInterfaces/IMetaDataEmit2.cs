using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

[NativeObject]
internal unsafe interface IMetaDataEmit2 : IMetaDataEmit
{
    public HResult DefineMethodSpec(
        [In] MdToken tkParent,
        [In] COR_SIGNATURE* pvSigBlob,
        [In] ulong cbSigBlob,
        [Out] out MdMethodSpec pmi);

    public HResult GetDeltaSaveSize(
        [In] CorSaveSize fSave,
        [Out] out DWORD pdwSaveSize);

    public HResult SaveDelta(
        [In] char* szFile,
        [In] DWORD dwSaveFlags);

    public HResult SaveDeltaToStream(
        [In] IntPtr pIStream,
        [In] DWORD dwSaveFlags);

    public HResult SaveDeltaToMemory(
        [Out] out IntPtr pbData,
        [In] ulong cbData);

    public HResult DefineGenericParam(
        [In] MdToken tk,
        [In] ulong ulParamSeq,
        [In] DWORD dwParamFlags,
        [In] char* szname,
        [In] DWORD reserved,
        // C-style zero-terminated array
        [In] MdToken* rtkConstraints,
        [Out] out MdGenericParam pgp);

    public HResult SetGenericParamProps(
        [In] MdGenericParam gp,
        [In] DWORD dwParamFlags,
        [In] char* szName,
        [In] DWORD reserved,
        // C-style zero-terminated array
        [In] MdToken* rtkConstraints);

    public HResult ResetENCLog();
}
