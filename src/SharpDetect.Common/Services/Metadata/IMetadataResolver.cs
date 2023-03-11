// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Services.Metadata
{
    public interface IMetadataResolver
    {
        bool TryGetModuleDef(ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out ModuleDef? module);
        bool TryGetTypeDef(TypeInfo typeInfo, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out TypeDef? typeDef);
        bool TryGetMethodDef(FunctionInfo functionInfo, ModuleInfo moduleInfo, bool resolveWrappers, [NotNullWhen(returnValue: true)] out MethodDef? methodDef);

        bool TryResolveTypeDef(IType type, [NotNullWhen(returnValue: true)] out TypeDef? typeDef);
        bool TryResolveMethodDef(IMethod method, [NotNullWhen(returnValue: true)] out MethodDef? methodDef);
        bool TryResolveFieldDef(IField field, [NotNullWhen(returnValue: true)] out FieldDef? fieldDef);

        bool TryGetWrapperMethodDefinition(WrapperMethodRefMDToken wrapperRef, ModuleInfo moduleInfo, out WrapperMethodDef wrapperMethodDef);
        bool TryGetExternMethodDefinition(WrapperMethodRefMDToken wrapperRef, ModuleInfo moduleInfo, out ExternMethodDef externMethodDef);
        bool TryGetWrapperMethodReference(MethodDef externMethod, ModuleInfo moduleInfo, out WrapperMethodRefMDToken reference);
        bool TryGetHelperMethodReference(MethodType helperType, ModuleInfo moduleInfo, out HelperMethodRefMDToken reference);
        bool TryLookupWrapperMethodReference(IMethodDefOrRef externMethod, ModuleInfo moduleInfo, out WrapperMethodRefMDToken reference);

        bool TryLookupTypeDef(string fullname, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out TypeDef? typeDef);
    }
}
