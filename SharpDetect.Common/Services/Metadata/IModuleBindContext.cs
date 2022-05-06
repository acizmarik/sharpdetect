﻿using dnlib.DotNet;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Services.Metadata
{
    public interface IModuleBindContext
    {
        ModuleDef LoadModule(int processId, string path, ModuleInfo moduleInfo);
        ModuleDef LoadModule(int processId, Stream stream, string virtualPath, ModuleInfo moduleInfo);

        ModuleDef GetModule(int processId, ModuleInfo moduleInfo);
        bool TryGetModule(int processId, ModuleInfo moduleInfo, [NotNullWhen(returnValue: true)] out ModuleDef? module);

        void UnloadAll();
    }
}
