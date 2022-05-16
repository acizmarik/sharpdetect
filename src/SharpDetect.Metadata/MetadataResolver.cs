using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Messages;
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

        public bool TryGetWrapperMethodReference(MethodDef externMethod, ModuleInfo moduleInfo, out MDToken reference)
        {
            return state.TryGetWrapperFromMethodReference(moduleInfo, externMethod, out reference);
        }

        public bool TryLookupWrapperMethodReference(IMethodDefOrRef externMethod, ModuleInfo moduleInfo, out MDToken reference)
        {
            return state.TryGetWrapperFromMethodReference(moduleInfo, externMethod, out reference);
        }

        public bool TryGetHelperMethodReference(MethodType helperType, ModuleInfo moduleInfo, out MDToken reference)
        {
            return state.TryGetHelperMethodReference(moduleInfo, helperType, out reference);
        }

        public bool TryResolveTypeDef(IType type, [NotNullWhen(returnValue: true)] out TypeDef? result)
        {
            Guard.NotNull<IType, ArgumentNullException>(type);

            // We have directly the reference to the definition
            if (type is TypeDef typeDef)
            {
                result = typeDef;
                return true;
            }

            // We have a reference that needs to be resolved
            if (type is TypeRef typeRef)
            {
                var typeDefOrNull = moduleBindContext.MetadataResolversProvider.MemberResolver.Resolve(typeRef);
                if (typeDefOrNull is TypeDef resolvedTypeDef)
                {
                    result = resolvedTypeDef;
                    return true;
                }
            }

            // Type specs need to be resolved to non-instantiated definitions
            if (type is TypeSpec typeSpec)
            {
                if (TryResolveTypeDef(typeSpec.ScopeType, out var resolvedTypeDef))
                {
                    result = resolvedTypeDef;
                    return true;
                }
            }

            // For some reason we were unable to resolve the token
            result = null;
            return false;
        }

        public bool TryResolveMethodDef(IMethod method, [NotNullWhen(returnValue: true)] out MethodDef? result)
        {
            Guard.NotNull<IMethod, ArgumentNullException>(method);

            // We have directly the reference to the definition
            if (method is MethodDef def)
            {
                result = def;
                return true;
            }

            // We have a reference that needs to be resolved
            if (method is MemberRef @ref)
            {
                if (!TryResolveTypeDef(method.DeclaringType, out var declaringTypeDef))
                {
                    result = null;
                    return false;
                }

                var methodDefOrNull = moduleBindContext.MetadataResolversProvider.MemberResolver.Resolve(@ref);
                if (methodDefOrNull is MethodDef resolvedMethodDef)
                {
                    result = resolvedMethodDef;
                    return true;
                }

                foreach (var methodDef in declaringTypeDef.Methods.Where(m => m.Name == method.Name))
                {
                    if (methodDef.ParamDefs.Count != method.MethodSig.Params.Count)
                        continue;

                    for (var paramIndex = 0; paramIndex < methodDef.ParamDefs.Count; ++paramIndex)
                    {
                        if (methodDef.MethodSig.Params[paramIndex].TypeName != method.MethodSig.Params[paramIndex].TypeName)
                            break;
                    }

                    result = methodDef;
                    return true;
                }
            }

            // Instantiated generic method
            if (method is MethodSpec spec)
            {
                // Resolve based on not instantiated method
                return TryResolveMethodDef(spec.Method, out result);
            }

            // For some reason we were unable to resolve the token
            result = null;
            return false;
        }

        public bool TryResolveFieldDef(IField field, [NotNullWhen(returnValue: true)] out FieldDef? result)
        {
            Guard.NotNull<IField, ArgumentNullException>(field);

            if (field is FieldDef fieldDef)
            {
                result = fieldDef;
                return true;
            }

            if (field is MemberRef memberRef)
            {
                var fieldDefOrNull = moduleBindContext.MetadataResolversProvider.MemberResolver.ResolveField(memberRef);
                if (fieldDefOrNull is FieldDef resolvedFieldDef)
                {
                    result = resolvedFieldDef;
                    return true;
                }
            }

            // For some reason we were unable to resolve the token
            result = null;
            return false;
        }
    }
}
