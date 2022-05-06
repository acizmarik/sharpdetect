using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Services.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Metadata
{
    internal class MetadataResolver : IMetadataResolver
    {
        public readonly int ProcessId;
        private readonly IModuleBindContext moduleBindContext;
        private readonly InjectedData state;

        public MetadataResolver(int processId, IModuleBindContext moduleBindContext, InjectedData state)
        {
            ProcessId = processId;

            this.moduleBindContext = moduleBindContext;
            this.state = state;
        }

        public bool TryGetModuleDef(ModuleInfo moduleInfo, [NotNullWhen(true)] out ModuleDef? module)
            => moduleBindContext.TryGetModule(ProcessId, moduleInfo, out module);

        public bool TryGetTypeDef(TypeInfo typeInfo, ModuleInfo moduleInfo, [NotNullWhen(true)] out TypeDef? typeDef)
        {
            typeDef = null;

            // Resolve module
            if (!TryGetModuleDef(moduleInfo, out var module))
                return false;

            // With biggest probability we find the method on the type def
            if ((typeDef = module.ResolveToken(typeInfo.TypeToken) as TypeDef) != null)
                return true;

            // Possibly this could be an injected type by SharpDetect
            if (state.TryGetInjectedType(moduleInfo, typeInfo.TypeToken, out typeDef))
                return true;

            // Otherwise this type was created dynamically and not by SharpDetect
            // TODO: add support for dynamic types
            return false;
        }

        public bool TryGetMethodDef(FunctionInfo functionInfo, ModuleInfo moduleInfo, [NotNullWhen(true)] out MethodDef? methodDef)
        {
            methodDef = null;

            // Resolve module
            if (!TryGetModuleDef(moduleInfo, out var module))
                return false;

            // With biggest probability the method can be found in the original module
            if ((methodDef = module.ResolveToken(functionInfo.FunctionToken) as MethodDef) != null)
                return true;

            // Possibly we could have wrapped this method
            if (state.TryGetMethodFromWrapperReference(moduleInfo, functionInfo.FunctionToken, out methodDef))
                return true;

            // Possibly this could be an injected method by SharpDetect
            if (state.TryGetInjectedMethod(moduleInfo, functionInfo.FunctionToken, out methodDef))
                return true;

            // Otherwise this method was created dynamically and not by SharpDetect
            // TODO: add support for dynamic methods
            return false;
        }

        public bool TryLookupTypeDef(string fullname, ModuleInfo moduleInfo, [NotNullWhen(true)] out TypeDef? typeDef)
        {
            typeDef = null;

            // Resolve module
            if (!TryGetModuleDef(moduleInfo, out var module))
                return false;

            // Resolve fullname
            if ((typeDef = module.Find(fullname, true)) == null)
                return false;

            return true;
        }
    }
}
