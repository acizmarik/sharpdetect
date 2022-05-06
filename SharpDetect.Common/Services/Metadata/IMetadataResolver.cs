using dnlib.DotNet;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Services.Metadata
{
    public interface IMetadataResolver
    {
        bool TryGetModuleDef(ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out ModuleDef? module);
        bool TryGetTypeDef(TypeInfo typeInfo, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out TypeDef? typeDef);
        bool TryGetMethodDef(FunctionInfo functionInfo, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out MethodDef? methodDef);

        bool TryLookupTypeDef(string fullname, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out TypeDef? typeDef);
    }
}
