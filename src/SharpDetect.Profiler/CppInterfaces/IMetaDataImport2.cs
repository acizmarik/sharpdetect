// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using ULONG = System.UInt32;

namespace SharpDetect.Profiler;

[NativeObject]
internal unsafe interface IMetaDataImport2 : IMetaDataImport
{
    HResult EnumGenericParams(
        HCORENUM* phEnum,
        MdToken tk,
        MdGenericParam* rGenericParams,
        ULONG cMax,
        out ULONG pcGenericParams);

    HResult GetGenericParamProps(
        MdGenericParam gp,
        out ULONG pulParamSeq,
        out DWORD pdwParamFlags,
        out MdToken ptOwner,
        out DWORD reserved,
        char* wzname,
        ULONG cchName,
        out ULONG pchName);

    HResult GetMethodSpecProps(
        MdMethodSpec mi,
        out MdToken tkParent,
        out IntPtr ppvSigBlob,
        out ULONG pcbSigBlob);

    HResult EnumGenericParamConstraints(
        IntPtr phEnum,
        MdGenericParam tk,
        MdGenericParamConstraint* rGenericParamConstraints,
        ULONG cMax,
        out ULONG pcGenericParamConstraints);

    HResult GetGenericParamConstraintProps(
        MdGenericParamConstraint gpc,
        out MdGenericParam ptGenericParam,
        out MdToken ptkConstraintType);

    HResult GetPEKind(
        out DWORD pdwPEKind,
        out DWORD pdwMAchine);

    HResult GetVersionString(
        char* pwzBuf,
        DWORD ccBufSize,
        out DWORD pccBufSize);

    HResult EnumMethodSpecs(
        IntPtr phEnum,
        MdToken tk,
        MdMethodSpec* rMethodSpecs,
        ULONG cMax,
        out ULONG pcMethodSpecs);
}