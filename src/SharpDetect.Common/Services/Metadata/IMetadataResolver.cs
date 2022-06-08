using dnlib.DotNet;
using SharpDetect.Common.Messages;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Services.Metadata
{
    public interface IMetadataResolver
    {
        ModuleDef WaitForModuleLoaded(ModuleInfo moduleInfo);

        bool TryGetModuleDef(ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out ModuleDef? module);
        bool TryGetTypeDef(TypeInfo typeInfo, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out TypeDef? typeDef);
        bool TryGetMethodDef(FunctionInfo functionInfo, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out MethodDef? methodDef);

        bool TryResolveTypeDef(IType type, [NotNullWhen(returnValue: true)] out TypeDef? typeDef);
        bool TryResolveMethodDef(IMethod method, [NotNullWhen(returnValue: true)] out MethodDef? methodDef);
        bool TryResolveFieldDef(IField field, [NotNullWhen(returnValue: true)] out FieldDef? fieldDef);

        bool TryGetWrapperMethodReference(MethodDef externMethod, ModuleInfo moduleInfo, out MDToken reference);
        bool TryGetHelperMethodReference(MethodType helperType, ModuleInfo moduleInfo, out MDToken reference);
        bool TryLookupWrapperMethodReference(IMethodDefOrRef externMethod, ModuleInfo moduleInfo, out MDToken reference);

        bool TryLookupTypeDef(string fullname, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out TypeDef? typeDef);
    }
}
