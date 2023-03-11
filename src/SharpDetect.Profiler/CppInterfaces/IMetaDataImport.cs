// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

[NativeObject]
internal unsafe interface IMetaDataImport : IUnknown
{
    void CloseEnum(HCORENUM hEnum);
    HResult CountEnum(HCORENUM hEnum, uint* pulCount);
    HResult ResetEnum(HCORENUM hEnum, uint ulPos);
    HResult EnumTypeDefs(HCORENUM* phEnum, MdTypeDef* rTypeDefs, uint cMax, uint* pcTypeDefs);
    HResult EnumInterfaceImpls(HCORENUM* phEnum, MdTypeDef td, MdInterfaceImpl* rImpls, uint cMax, uint* pcImpls);
    HResult EnumTypeRefs(HCORENUM* phEnum, MdTypeRef* rTypeRefs, uint cMax, uint* pcTypeRefs);

    HResult FindTypeDefByName(
        char* szTypeDef,
        MdToken tkEnclosingClass,
        out MdTypeDef ptd);

    HResult GetScopeProps(
        char* szName,
        uint cchName,
        out uint pchName,
        out Guid pmvid);

    HResult GetModuleFromScope(
        out MdModule pmd);

    HResult GetTypeDefProps(
        MdTypeDef td,
        char* szTypeDef,
        uint cchTypeDef,
        out uint pchTypeDef,
        out int pdwTypeDefFlags,
        out MdToken ptkExtends);

    HResult GetInterfaceImplProps(
        MdInterfaceImpl iiImpl,
        out MdTypeDef pClass,
        out MdToken ptkIface);

    HResult GetTypeRefProps(
        MdTypeRef tr,
        out MdToken* ptkResolutionScope,
        char* szName,
        uint cchName,
        out uint pchName);

    HResult ResolveTypeRef(
        MdTypeRef tr, 
        in Guid riid, 
        void** ppIScope, 
        out MdTypeDef ptd);

    HResult EnumMembers(
        HCORENUM* phEnum,
        MdTypeDef cl,
        MdToken* rMembers,
        uint cMax,
        out uint pcTokens);

    HResult EnumMembersWithName(
        HCORENUM* phEnum,
        MdTypeDef cl,
        char* szName,
        MdToken* rMembers,
        uint cMax,
        out uint pcTokens);

    HResult EnumMethods(
        HCORENUM* phEnum,
        MdTypeDef cl,
        MdMethodDef* rMethods,
        uint cMax,
        out uint pcTokens);

    HResult EnumMethodsWithName(
        HCORENUM* phEnum,
        MdTypeDef cl,
        char* szName,
        MdMethodDef* rMethods,
        uint cMax,
        out uint pcTokens);

    HResult EnumFields(
        HCORENUM* phEnum,
        MdTypeDef cl,
        MdFieldDef* rFields,
        uint cMax,
        out uint pcTokens);

    HResult EnumFieldsWithName(
        HCORENUM* phEnum,
        MdTypeDef cl,
        char* szName,
        MdFieldDef* rFields,
        uint cMax,
        out uint pcTokens);


    HResult EnumParams(
        HCORENUM* phEnum,
        MdMethodDef mb,
        MdParamDef* rParams,
        uint cMax,
        out uint pcTokens);

    HResult EnumMemberRefs(
        HCORENUM* phEnum,
        MdToken tkParent,
        MdMemberRef* rMemberRefs,
        uint cMax,
        out uint pcTokens);

    HResult EnumMethodImpls(
        HCORENUM* phEnum,
        MdTypeDef td,
        MdToken* rMethodBody,
        MdToken* rMethodDecl,
        uint cMax,
        out uint pcTokens);

    HResult EnumPermissionSets(
        HCORENUM* phEnum,
        MdToken tk,
        int dwActions,
        MdPermission* rPermission,
        uint cMax,
        out uint pcTokens);

    HResult FindMember(
        MdTypeDef td,
        char* szName,
        COR_SIGNATURE* pvSigBlob,
        ulong cbSigBlob,
        out MdToken pmb);

    HResult FindMethod(
        MdTypeDef td,
        char* szName,
        COR_SIGNATURE* pvSigBlob,
        ulong cbSigBlob,
        out MdMethodDef pmb);

    HResult FindField(
        MdTypeDef td,
        char* szName,
        COR_SIGNATURE* pvSigBlob,
        ulong cbSigBlob,
        out MdFieldDef pmb);

    HResult FindMemberRef(
        MdTypeRef td,
        char* szName,
        COR_SIGNATURE* pvSigBlob,
        ulong cbSigBlob,
        out MdMemberRef pmr);

    HResult GetMethodProps(
        MdMethodDef mb,
        out MdTypeDef pClass,
        char* szMethod,
        uint cchMethod,
        out uint pchMethod,
        out uint pdwAttr,
        out COR_SIGNATURE* ppvSigBlob,
        out uint pcbSigBlob,
        out uint pulCodeRVA,
        out int pdwImplFlags);

    HResult GetMemberRefProps(
        MdMemberRef mr,
        out MdToken ptk,
        char* szMember,
        uint cchMember,
        out uint pchMember,
        out COR_SIGNATURE* ppvSigBlob,
        out uint pbSig);

    HResult EnumProperties(
        HCORENUM* phEnum,
        MdTypeDef td,
        MdProperty* rProperties,
        uint cMax,
        out uint pcProperties);

    HResult EnumEvents(
        HCORENUM* phEnum,
        MdTypeDef td,
        MdEvent* rEvents,
        uint cMax,
        out uint pcEvents);

    HResult GetEventProps(
        MdEvent ev,
        MdTypeDef* pClass,
        char* szEvent,
        uint cchEvent,
        uint* pchEvent,
        int* pdwEventFlags,
        MdToken* ptkEventType,
        out MdMethodDef pmdAddOn,
        out MdMethodDef pmdRemoveOn,
        out MdMethodDef pmdFire,
        out MdMethodDef* rmdOtherMethod,
        uint cMax,
        out uint pcOtherMethod);

    HResult EnumMethodSemantics(
        HCORENUM* phEnum,
        MdMethodDef mb,
        out MdToken* rEventProp,
        uint cMax,
        out uint pcEventProp);

    HResult GetMethodSemantics(
        MdMethodDef mb,
        MdToken tkEventProp,
        out int pdwSemanticsFlags);

    HResult GetClassLayout(
        MdTypeDef td,
        out int pdwPackSize,
        COR_FIELD_OFFSET* rFieldOffset,
        uint cMax,
        out uint pcFieldOffset,
        out uint pulClassSize);

    HResult GetFieldMarshal(
        MdToken tk,
        out nint* ppvNativeType,
        out uint pcbNativeType);

    HResult GetRVA(
        MdToken tk,
        uint* pulCodeRVA,
        int* pdwImplFlags);

    HResult GetPermissionSetProps(
        MdPermission pm,
        out int pdwAction,
        out void* ppvPermission,
        out uint pcbPermission);

    HResult GetSigFromToken(
        MdSignature mdSig,
        out COR_SIGNATURE* ppvSig,
        out uint pcbSig);

    HResult GetModuleRefProps(
        MdModuleRef mur,
        char* szName,
        uint cchName,
        out uint pchName);

    HResult EnumModuleRefs(
        HCORENUM* phEnum,
        MdModuleRef* rModuleRefs,
        uint cmax,
        out uint pcModuleRefs);

    HResult GetTypeSpecFromToken(
        MdTypeSpec typespec,
        out COR_SIGNATURE* ppvSig,
        out uint pcbSig);

    HResult GetNameFromToken(
        MdToken tk,
        out byte* pszUtf8NamePtr);

    HResult EnumUnresolvedMethods(
        HCORENUM* phEnum,
        MdToken* rMethods,
        uint cMax,
        out uint pcTokens);

    HResult GetUserString(
        MdString stk,
        char* szString,
        uint cchString,
        out uint pchString);

    HResult GetPinvokeMap(
        MdToken tk,
        out int pdwMappingFlags,
        char* szImportName,
        uint cchImportName,
        out uint pchImportName,
        out MdModuleRef pmrImportDLL);

    HResult EnumSignatures(
        HCORENUM* phEnum,
        MdSignature* rSignatures,
        uint cmax,
        out uint pcSignatures);

    HResult EnumTypeSpecs(
        HCORENUM* phEnum,
        MdTypeSpec* rTypeSpecs,
        uint cmax,
        out uint pcTypeSpecs);

    HResult EnumUserStrings(
        HCORENUM* phEnum,
        MdString* rStrings,
        uint cmax,
        out uint pcStrings);

    HResult GetParamForMethodIndex(
        MdMethodDef md,
        uint ulParamSeq,
        out MdParamDef ppd);

    HResult EnumCustomAttributes(
        HCORENUM* phEnum,
        MdToken tk,
        MdToken tkType,
        MdCustomAttribute* rCustomAttributes,
        uint cMax,
        out uint pcCustomAttributes);

    HResult GetCustomAttributeProps(
        MdCustomAttribute cv,
        out MdToken ptkObj,
        out MdToken ptkType,
        out void* ppBlob,
        out uint pcbSize);

    HResult FindTypeRef(
        MdToken tkResolutionScope,
        char* szName,
        out MdTypeRef ptr);

    HResult GetMemberProps(
        MdToken mb,
        MdTypeDef* pClass,
        char* szMember,
        uint cchMember,
        uint* pchMember,
        int* pdwAttr,
        out COR_SIGNATURE* ppvSigBlob,
        out uint pcbSigBlob,
        out uint pulCodeRVA,
        int* pdwImplFlags,
        int* pdwCPlusTypeFlag,
        out byte ppValue,
        out uint pcchValue);

    HResult GetFieldProps(
        MdFieldDef mb,
        MdTypeDef* pClass,
        char* szField,
        uint cchField,
        uint* pchField,
        int* pdwAttr,
        out COR_SIGNATURE* ppvSigBlob,
        out uint pcbSigBlob,
        out int pdwCPlusTypeFlag,
        out byte ppValue,
        out uint pcchValue);

    HResult GetPropertyProps(
        MdProperty prop,
        out MdTypeDef pClass,
        char* szProperty,
        uint cchProperty,
        out uint pchProperty,
        out int pdwPropFlags,
        out COR_SIGNATURE* ppvSig,
        out uint pbSig, 
        out int pdwCPlusTypeFlag,
        out byte ppDefaultValue,
        out uint pcchDefaultValue,
        out MdMethodDef pmdSetter,
        out MdMethodDef pmdGetter,
        out MdMethodDef rmdOtherMethod,
        uint cMax,
        out uint pcOtherMethod);

    HResult GetParamProps(
        MdParamDef tk,
        out MdMethodDef pmd,
        out uint pulSequence,
        char* szName,
        uint cchName,
        out uint pchName,
        out int pdwAttr,
        out int pdwCPlusTypeFlag,
        out byte ppValue,
        out uint pcchValue);

    HResult GetCustomAttributeByName(
        MdToken tkObj,
        char* szName,
        out void* ppData,
        out uint pcbData);

    bool IsValidToken(
        MdToken tk);

    HResult GetNestedClassProps(
        MdTypeDef tdNestedClass,
        out MdTypeDef ptdEnclosingClass);

    HResult GetNativeCallConvFromSig(
        COR_SIGNATURE* pvSig,
        uint cbSig,
        out uint pCallConv);

    HResult IsGlobal(
        MdToken pd,
        out int pbGlobal);
}