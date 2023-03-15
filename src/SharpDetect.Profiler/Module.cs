// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Profiler.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SharpDetect.Profiler;

internal unsafe class Module
{
    public readonly ModuleId Id;
    public readonly string Name;
    public readonly AssemblyId AssemblyId;
    public readonly string FullPath;
    private readonly IMetaDataImport2 metadataModuleImport;
    private readonly IMetaDataEmit2 metadataModuleEmit;
    private readonly IMethodAlloc methodAlloc;
    private MdTypeDef? objectTypeDef;
    private MdMethodDef? objectCtorMethodDef;
    private uint? objectCtorRVA;

    public Module(ModuleId moduleId, ICorProfilerInfo3 profilerInfo)
    {
        // Obtain IMetaDataImport2
        var imetadataImport2Guid = KnownGuids.IMetaDataImport2;
        if (!profilerInfo.GetModuleMetaData(moduleId, CorOpenFlags.ofRead, &imetadataImport2Guid, out var ppOut))
            throw new ArgumentException($"Could not obtain {nameof(KnownGuids.IMetaDataImport2)}");
        metadataModuleImport = NativeObjects.IMetaDataImport2.Wrap(ppOut);

        // Obtain IMetaDataEmit2
        var imetadataEmit2Guid = KnownGuids.IMetaDataEmit2;
        if (!profilerInfo.GetModuleMetaData(moduleId, CorOpenFlags.ofRead | CorOpenFlags.ofWrite, &imetadataEmit2Guid, out ppOut))
            throw new ArgumentException($"Could not obtain {nameof(KnownGuids.IMetaDataEmit2)}");
        metadataModuleEmit = NativeObjects.IMetaDataEmit2.Wrap(ppOut);

        // Obtain IMethodAlloc
        if (!profilerInfo.GetILFunctionBodyAllocator(moduleId, out ppOut))
            throw new ArgumentException($"Could not obtain {nameof(IMethodAlloc)}");
        methodAlloc = NativeObjects.IMethodAlloc.Wrap(ppOut);

        // Obtain module's path
        // First, get length of module path
        if (!profilerInfo.GetModuleInfo2(moduleId, out _, 0, out var pathLength, null, out var assemblyId, out _))
            throw new ArgumentException("Could not obtain information about module's full path");
        
        // Second, get the module path
        Span<char> buffer = stackalloc char[(int)pathLength];
        fixed (char* ptr = buffer)
        {
            if (!profilerInfo.GetModuleInfo2(moduleId, out _, pathLength, out _, ptr, out _, out _))
                throw new ArgumentException("Could not obtain full path of module");
            FullPath = new string(ptr, 0, (int)(pathLength - 1));
        }

        // Store module information
        Id = moduleId;
        AssemblyId = assemblyId;
        Name = Path.GetFileName(FullPath);
    }

    public HResult AddTypeDef(
        string name, 
        CorTypeAttr flags, 
        MdToken baseType, 
        out MdTypeDef typeDef)
    {
        fixed (char* ptr = name)
        {
            return metadataModuleEmit.DefineTypeDef(ptr, flags, baseType, null, out typeDef);
        }
    }

    public HResult AddMethodDef(
        string name,
        CorMethodAttr flags,
        MdTypeDef typeDef,
        in ReadOnlySpan<COR_SIGNATURE> signature,
        CorMethodImpl implFlags,
        out MdMethodDef methodDefinition)
    {
        methodDefinition = MdMethodDef.Nil;
        if (!GetPlaceHolderMethodRVA(out var rva))
            return HResult.E_FAIL;

        fixed (char* namePtr = name)
        {
            fixed (COR_SIGNATURE* sigPtr = signature)
            {
                var cbSig = (ulong)signature.Length;
                if (!metadataModuleEmit.DefineMethod(typeDef, namePtr, flags, sigPtr, cbSig, rva, implFlags, out methodDefinition))
                    return HResult.E_FAIL;
            }
        }

        return HResult.S_OK;
    }

    public HResult AddMethodRef(
        string name,
        MdTypeRef typeRef,
        in Span<COR_SIGNATURE> signature,
        out MdMemberRef memberReference)
    {
        fixed (char* namePtr = name)
        {
            fixed (COR_SIGNATURE* sigPtr = signature)
            {
                var cbSig = (ulong)signature.Length;
                var mdToken = Unsafe.As<MdTypeRef, MdToken>(ref typeRef);
                if (!metadataModuleEmit.DefineMemberRef(mdToken, namePtr, sigPtr, cbSig, out memberReference))
                    return HResult.E_FAIL;
            }
        }

        return HResult.S_OK;
    }

    public IntPtr AllocMethodBody(ulong size)
    {
        return methodAlloc.Alloc(size);
    }

    public HResult AddTypeRef(
        MdAssemblyRef declaringAssemblyRef,
        string typeName,
        out MdTypeRef typeRef)
    {
        fixed (char* ptr = typeName)
        {
            var mdToken = Unsafe.As<MdAssemblyRef, MdToken>(ref declaringAssemblyRef);
            return metadataModuleEmit.DefineTypeRefByName(mdToken, ptr, out typeRef);
        }
    }

    public HResult FindTypeDef(string name, out MdTypeDef typeDef)
    {
        fixed (char* ptr = name)
        {
            return metadataModuleImport.FindTypeDefByName(ptr, MdToken.Nil, out typeDef);
        }
    }

    public HResult FindTypeRef(MdAssemblyRef assemblyRef, string name, out MdTypeRef typeDef)
    {
        fixed (char* ptr = name)
        {
            var mdToken = Unsafe.As<MdAssemblyRef, MdToken>(ref assemblyRef);
            return metadataModuleImport.FindTypeRef(mdToken, ptr, out typeDef);
        }
    }

    public HResult FindMethodDef(string name, in Span<COR_SIGNATURE> signature, MdTypeDef typeDef, out MdMethodDef methodDef)
    {
        fixed (char* namePtr = name)
        {
            fixed (COR_SIGNATURE* sigPtr = signature)
            {
                return metadataModuleImport.FindMethod(typeDef, namePtr, sigPtr, (ulong)signature.Length, out methodDef);
            }
        }
    }

    public HResult FindMethodRef(string name, in Span<COR_SIGNATURE> signature, MdTypeRef typeRef, out MdMemberRef methodRef)
    {
        fixed (char* namePtr = name)
        {
            fixed (COR_SIGNATURE* sigPtr = signature)
            {
                return metadataModuleImport.FindMemberRef(typeRef, namePtr, sigPtr, (ulong)signature.Length, out methodRef);
            }
        }
    }

    public HResult GetMethodProps(
        MdMethodDef methodDef,
        [NotNullWhen(returnValue: default)] out MdTypeDef typeDef,
        [NotNullWhen(returnValue: default)] out string? name,
        [NotNullWhen(returnValue: default)] out CorMethodAttr? flags, 
        [NotNullWhen(returnValue: default)] out ReadOnlySpan<COR_SIGNATURE> signature)
    {
        typeDef = MdTypeDef.Nil;
        name = null;
        flags = null;
        signature = null;

        // Obtain method name and signature length
        if (!metadataModuleImport.GetMethodProps(methodDef, out typeDef, null, 0, out var nameLength, out _, out _, out var signatureLength, out _, out _))
            return HResult.E_FAIL;

        // Retrieve data
        COR_SIGNATURE* signaturePtr = null;
        Span<char> bufferName = stackalloc char[(int)nameLength];
        fixed (char* namePtr = bufferName)
        {
            if (!metadataModuleImport.GetMethodProps(methodDef, out _, namePtr, nameLength, out _, out var methodFlags, out signaturePtr, out _, out _, out _))
                return HResult.E_FAIL;

            flags = (CorMethodAttr?)methodFlags;
        }

        name = new string(bufferName);
        signature = new ReadOnlySpan<COR_SIGNATURE>(signaturePtr, (int)signatureLength);
        return HResult.S_OK;
    }

    public HResult GetTypeProps(
        MdTypeDef typeDef,
        [NotNullWhen(returnValue: true)] out string? name)
    {
        name = null;

        // Obtain type name length
        if (!metadataModuleImport.GetTypeDefProps(typeDef, null, 0, out var nameLength, out _, out _))
            return HResult.E_FAIL;

        // Retrieve data
        Span<char> bufferName = stackalloc char[(int)nameLength];
        fixed (char* namePtr = bufferName)
        {
            if (!metadataModuleImport.GetTypeDefProps(typeDef, namePtr, nameLength, out _, out _, out _))
                return HResult.E_FAIL;
        }

        name = new string(bufferName);
        return HResult.S_OK;
    }

    private HResult GetPlaceHolderMethodRVA(out uint rva)
    {
        // Note: for some reason CLR does not like receiving 0 as RVA for a method
        // We will provide the implementation later, until then place there System.Object::ctor() to make it happy
        rva = 0;
        const string systemObjectName = "System.Object";
        const string ctorMethodName = ".ctor";

        if (!objectTypeDef.HasValue)
        {
            if (!FindTypeDef(systemObjectName, out var objectTypeDef))
            {
                Logger.LogError($"Could not find type definition for \"{systemObjectName}\"");
                return HResult.E_FAIL;
            }

            this.objectTypeDef = objectTypeDef;
        }

        if (!objectCtorMethodDef.HasValue)
        {
            Span<COR_SIGNATURE> objectCtorSignature = stackalloc COR_SIGNATURE[]
            {
                // Calling convention
                (byte)CorCallingConvention.IMAGE_CEE_CS_CALLCONV_HASTHIS,
                // Arguments count
                0,
                // Return value
                (byte)CorElementType.ELEMENT_TYPE_VOID
            };

            if (!FindMethodDef(ctorMethodName, objectCtorSignature, objectTypeDef.Value, out var objectCtorMethodDef))
            {
                Logger.LogError($"Could not find method definition for \"{systemObjectName}::{ctorMethodName}()\"");
                return HResult.E_FAIL;
            }

            this.objectCtorMethodDef = objectCtorMethodDef;
        }

        if (!objectCtorRVA.HasValue)
        {
            if (!metadataModuleImport.GetMethodProps(objectCtorMethodDef.Value, out _, (char*)0, 0, out _, out _, out _, out _, out rva, out _))
            {
                Logger.LogError($"Could not obtain \"{systemObjectName}::{ctorMethodName}()\"'s RVA");
                return HResult.E_FAIL;
            }

            this.objectCtorRVA = rva;
        }

        rva = objectCtorRVA.Value;
        return HResult.S_OK;
    }
}
