using dnlib.DotNet;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Services.Metadata
{
    public interface IModuleBindContext
    {
        IMetadataResolversProvider MetadataResolversProvider { get; }

        ModuleDef LoadModule(int processId, string path, ModuleInfo moduleInfo);
        ModuleDef LoadModule(int processId, Stream stream, string virtualPath, ModuleInfo moduleInfo);

        ModuleDef GetModule(int processId, ModuleInfo moduleInfo);
        ModuleDef GetCoreLibModule(int processId);
        bool TryGetModule(int processId, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out ModuleDef? module);
        bool TryGetCoreLibModule(int processId, [NotNullWhen(returnValue: true)] out ModuleDef? module);

        void UnloadAll();
    }
}
