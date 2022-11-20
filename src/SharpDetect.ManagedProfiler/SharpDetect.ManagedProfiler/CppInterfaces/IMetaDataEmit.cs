using System.Runtime.InteropServices;

namespace SharpDetect.Profiler
{
    [NativeObject]
    internal unsafe interface IMetaDataEmit : IUnknown
    {
        public HResult SetModuleProps(
            [In] char* szName);

        public HResult Save(
            [In] char* szFile,
            [In] DWORD dwSaveFlags);

        public HResult SaveToStream(
            [In] IntPtr pIStream,
            [In] DWORD dwSaveFlags);

        public HResult GetSaveSize(
            [In] CorSaveSize fSave,
            [Out] out DWORD pdwSaveSize);

        public HResult DefineTypeDef(
            [In] char* szTypeDef,
            [In] CorTypeAttr dwTypeDefFlags,
            [In] MdToken tkExtends,
            // C-style array terminated by mdTokenNil
            [In] MdToken* rtkImplements,
            [Out] out MdTypeDef ptd);

        public HResult DefineNestedType(
            [In] char* szTypeDef,
            [In] DWORD dwTypeDefFlags,
            [In] MdToken tkExtends,
            // C-style array terminated by mdTokenNil
            [In] MdToken* rtkImplements,
            [In] MdTypeDef tdEncloser,
            [Out] out MdTypeDef ptd);

        public HResult SetHandler(
            [In] IntPtr pUnk);

        public HResult DefineMethod(
            [In] MdTypeDef td,
            [In] char* szName,
            [In] CorMethodAttr dwMethodFlags,
            [In] COR_SIGNATURE* pvSigBlob,
            [In] ulong cbSigBlob,
            [In] ulong ulCodeRVA,
            [In] CorMethodImpl dwImplFlags,
            [Out] out MdMethodDef pmd);

        public HResult DefineMethodImpl(
            [In] MdTypeDef td,
            [In] MdToken tkBody,
            [In] MdToken tkDecl);

        public HResult DefineTypeRefByName(
            [In] MdToken tkResolutionScope,
            [In] char* szName,
            [Out] out MdTypeRef ptr);

        public HResult DefineImportType(
            [In] IntPtr pAssemImport,
            [In] IntPtr pbHashValue,
            [In] ulong cbHashValue,
            [In] IntPtr pImport,
            [In] MdTypeDef tdImport,
            [In] IntPtr pAssemEmit,
            [Out] out MdTypeRef ptr);

        public HResult DefineMemberRef(
            [In] MdToken tkImport,
            [In] char* szName,
            [In] COR_SIGNATURE* pvSigBlob,
            [In] ulong cvSigBlob,
            [Out] out MdMemberRef pmr);

        public HResult DefineImportMember(
            [In] IntPtr pAssemImport,
            [In] IntPtr pbHashValue,
            [In] ulong cbHashValue,
            [In] IntPtr pImport,
            [In] MdToken mbMember,
            [In] IntPtr pAssemEmit,
            [In] MdToken tkParent,
            [Out] out MdMemberRef pmr);

        public HResult DefineEvent(
            [In] MdTypeDef td,
            [In] char* szEvent,
            [In] DWORD dwEventFlags,
            [In] MdToken tkEventType,
            [In] MdMethodDef mdAddOn,
            [In] MdMethodDef mdRemoveOn,
            [In] MdMethodDef mdFire,
            // C-style array terminated by mdMethodDefNil
            [In] MdMethodDef* rmdOtherMethods,
            [Out] out MdEvent pmdEvent);

        public HResult SetClassLayout(
            [In] MdTypeDef td,
            [In] DWORD dwPackSize,
            // C-style array terminated with mdTokenNil
            [In] COR_FIELD_OFFSET* rFieldOffsets,
            [In] ulong ulClassSize);

        public HResult DeleteClassLayout(
            [In] MdTypeDef td);

        public HResult SetFieldMarshal(
            [In] MdToken tk,
            [In] COR_SIGNATURE* pvNativeType,
            [In] ulong cbNativeType);

        public HResult DeleteFieldMarshal(
            [In] MdToken tk);

        public HResult DefinePermissionSet(
            [In] MdToken tk,
            [In] DWORD dwAction,
            [In] IntPtr pvPermission,
            [In] ulong cbPermission,
            [Out] out MdPermission ppm);

        public HResult SetRVA(
            [In] MdMethodDef md,
            [In] ulong ulRVA);

        public HResult GetTokenFromSig(
            [In] COR_SIGNATURE* pvSig,
            [In] ulong cbSig,
            [Out] out MdSignature pmsig);

        public HResult DefineModuleRef(
            [In] char* szName,
            [Out] out MdModuleRef pmur);

        public HResult SetParent(
            [In] MdMemberRef mr,
            [In] MdToken tk);

        public HResult GetTokenFromTypeSpec(
            [In] COR_SIGNATURE* pvSig,
            [In] ulong cbSig,
            [Out] out MdTypeSpec ptypespec);

        public HResult SaveToMemory(
            [Out] out IntPtr pbData,
            [In] ulong cbData);

        public HResult DefineUserString(
            [In] char* szString,
            [In] ulong cchString,
            [Out] out MdString pstk);

        public HResult DeleteToken(
            [In] MdToken tkObj);

        public HResult SetMethodProps(
            [In] MdMethodDef md,
            [In] DWORD dwMethodFlags,
            [In] ulong ulCodeRVA,
            [In] DWORD dwImplFlags);

        public HResult SetTypeDefProps(
            [In] MdTypeDef td,
            [In] DWORD dwTypeDefFlags,
            [In] MdToken tkExtends,
            // C-style array terminated with mdTokenNil
            [In] MdToken* rtkImplements);

        public HResult SetEventProps(
            [In] MdEvent ev,
            [In] DWORD dwEventFlags,
            [In] MdToken tkEventType,
            [In] MdMethodDef mdAddOn,
            [In] MdMethodDef mdRemoveOn,
            [In] MdMethodDef mdFire,
            // C-style array terminated with mdMethodDefNil
            [In] MdMethodDef* rmdOtherMethods);

        public HResult SetPermissionSetProps(
            [In] MdToken tk,
            [In] DWORD dwAction,
            [In] IntPtr pvPermission,
            [In] ulong cbPermission,
            [Out] out MdPermission ppm);

        public HResult DefinePinvokeMap(
            [In] MdToken tk,
            [In] DWORD dwMappingFlags,
            [In] char* szImportName,
            [In] MdModuleRef mrImportDLL);

        public HResult SetPinvokeMap(
            [In] MdToken tk,
            [In] DWORD dwMappingFlags,
            [In] char* szImportName,
            [In] MdModuleRef mrImportDLL);

        public HResult DeletePinvokeMap(
            [In] MdToken tk);

        public HResult DefineCustomAttribute(
            [In] MdToken tkObj,
            [In] MdToken tkType,
            [In] IntPtr pCustomAttribute,
            [In] ulong cbCustomAttribute,
            [Out] out MdCustomAttribute pcv);

        public HResult SetCustomAttributeValue(
            [In] MdCustomAttribute pcv,
            [In] IntPtr pCustomAttribute,
            [In] ulong cbCustomAttribute);

        public HResult DefineField(
            [In] MdTypeDef td,
            [In] char* szName,
            [In] DWORD dwFieldFlags,
            [In] COR_SIGNATURE* pvSigBlob,
            [In] ulong cbSigBlob,
            [In] DWORD dwCPlusTypeFlag,
            [In] IntPtr pValue,
            [In] ulong cchValue,
            [Out] out MdFieldDef pmd);

        public HResult DefineProperty(
            [In] MdTypeDef td,
            [In] char* szProperty,
            [In] DWORD dwPropFlags,
            [In] COR_SIGNATURE* pvSig,
            [In] ulong cbSig,
            [In] DWORD dwCPlusTypeFlag,
            [In] IntPtr pValue,
            [In] ulong cchValue,
            [In] MdMethodDef mdSetter,
            [In] MdMethodDef mdGetter,
            // C-style array terminated by mdTokenNil
            [In] MdMethodDef* rmdOtherMethods,
            [Out] out MdProperty pmdProp);

        public HResult DefineParam(
            [In] MdMethodDef md,
            [In] ulong ulParamSeq,
            [In] char* szName,
            [In] DWORD dwParamFlags,
            [In] DWORD dwCPlusTypeFlag,
            [In] IntPtr pValue,
            [In] ulong cchValue,
            [Out] out MdParamDef ppd);

        public HResult SetFieldProps(
            [In] MdFieldDef fd,
            [In] DWORD dwFieldFlags,
            [In] DWORD dwCPlusTypeFlag,
            [In] IntPtr pValue,
            [In] ulong cchValue);

        public HResult SetPropertyProps(
            [In] MdProperty pr,
            [In] DWORD dwPropFlags,
            [In] DWORD dwCPlusTypeFlag,
            [In] IntPtr pValue,
            [In] ulong cchValue,
            [In] MdMethodDef mdSetter,
            [In] MdMethodDef mdGetter,
            // C-style array terminated with mdTokenNil
            [In] MdMethodDef* rmdOtherMethods);

        public HResult SetParamProps(
            [In] MdParamDef pd,
            [In] char* szName,
            [In] DWORD dwParamFlags,
            [In] DWORD dwCPlusTypeFlag,
            [In] IntPtr pValue,
            [In] ulong cchValue);

        public HResult DefineSecurityAttributeSet(
            [In] MdToken tkObj,
            // C-style array whose size is determinated by the cSecAttrs
            [In] COR_SECATTR* rSecAttrs,
            [In] ulong cSecAttrs,
            [Out] out ulong pulErrorAttr);

        public HResult ApplyEditAndContinue(
            [In] IntPtr pImport);

        public HResult TranslateSigWithScope(
            [In] IntPtr pAssemImport,
            [In] IntPtr pbHashValue,
            [In] ulong cbHashValue,
            [In] IntPtr import,
            [In] COR_SIGNATURE* pbSigBlob,
            [In] ulong cbSigBlob,
            [In] IntPtr pAssemEmit,
            [In] IntPtr emit,
            [Out] out COR_SIGNATURE* pvTranslatedSig,
            [In] ulong cbTranslatedSigMax,
            [Out] out ulong* pcbTranslatedSig);

        public HResult SetMethodImplFlags(
            [In] MdMethodDef md,
            [In] DWORD dwImplFlags);

        public HResult SetFieldRVA(
            [In] MdFieldDef fd,
            [In] ulong ulRVA);

        public HResult Merge(
            [In] IntPtr pImport,
            [In] IntPtr pHostMapToken,
            [In] IntPtr pHandler);

        public HResult MergeEnd();
    }
}
