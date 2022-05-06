using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Metadata
{
    internal sealed class InjectedData
    {
        public readonly int ProcessId;

        private ImmutableDictionary<ModuleInfo, MDToken> eventDispatcherReferences;
        private ImmutableDictionary<ModuleInfo, Dictionary<MethodType, MDToken>> helperReferences;
        private ImmutableDictionary<ModuleInfo, Dictionary<MethodDef, MDToken>> externMethodToWrapperReferenceMapping;
        private ImmutableDictionary<ModuleInfo, Dictionary<MDToken, MethodDef>> wrapperReferenceToExternMethodMapping;
        private ImmutableDictionary<ModuleInfo, Dictionary<MDToken, TypeDef>> newTypes;
        private ImmutableDictionary<ModuleInfo, Dictionary<MDToken, MethodDef>> newMethods;

        public InjectedData(int processId)
        {
            ProcessId = processId;
            eventDispatcherReferences = ImmutableDictionary<ModuleInfo, MDToken>.Empty;
            helperReferences = ImmutableDictionary<ModuleInfo, Dictionary<MethodType, MDToken>>.Empty;
            externMethodToWrapperReferenceMapping = ImmutableDictionary<ModuleInfo, Dictionary<MethodDef, MDToken>>.Empty;
            wrapperReferenceToExternMethodMapping = ImmutableDictionary<ModuleInfo, Dictionary<MDToken, MethodDef>>.Empty;
            newTypes = ImmutableDictionary<ModuleInfo, Dictionary<MDToken, TypeDef>>.Empty;
            newMethods = ImmutableDictionary<ModuleInfo, Dictionary<MDToken, MethodDef>>.Empty;
        }

        public void AddTypeDef(ModuleInfo owner, TypeDef definition, MDToken token)
        {
            if (!newTypes.ContainsKey(owner))
                newTypes = newTypes.Add(owner, new());

            newTypes[owner].Add(token, definition);
        }

        public void AddMethodDef(ModuleInfo owner, MethodDef definition, MDToken token)
        {
            if (!newMethods.ContainsKey(owner))
                newMethods = newMethods.Add(owner, new());

            newMethods[owner].Add(token, definition);
        }

        public void AddMethodReference(ModuleInfo importer, MethodDef definition, MDToken reference)
        {
            if (!externMethodToWrapperReferenceMapping.ContainsKey(importer))
            {
                externMethodToWrapperReferenceMapping = externMethodToWrapperReferenceMapping.Add(importer, new());
                wrapperReferenceToExternMethodMapping = wrapperReferenceToExternMethodMapping.Add(importer, new());
            }

            wrapperReferenceToExternMethodMapping[importer].Add(reference, definition);
            externMethodToWrapperReferenceMapping[importer].Add(definition, reference);
        }

        public void AddMethodReference(ModuleInfo importer, MethodType type, MDToken reference)
        {
            if (!helperReferences.ContainsKey(importer))
                helperReferences = helperReferences.Add(importer, new());

            helperReferences[importer].Add(type, reference);
        }

        public void AddEventDispatcherReference(ModuleInfo importer, MDToken reference)
        {
            eventDispatcherReferences = eventDispatcherReferences.Add(importer, reference);
        }

        public bool TryGetInjectedType(ModuleInfo module, MDToken token, [NotNullWhen(returnValue: true)] out TypeDef? typeDef)
        {
            return newTypes[module].TryGetValue(token, out typeDef);
        }

        public bool TryGetInjectedMethod(ModuleInfo module, MDToken token, [NotNullWhen(returnValue: true)] out MethodDef? methodDef)
        {
            return newMethods[module].TryGetValue(token, out methodDef);
        }

        public bool TryGetHelperMethodReference(ModuleInfo module, MethodType type, out MDToken token)
        {
            /*  Data-race is not possible here
             *     - the collection is modified only during ModuleLoadFinished event
             *     - once we access mapping data, it is no longer modified */
            return helperReferences[module].TryGetValue(type, out token);
        }

        public bool TryGetWrapperFromMethodReference(ModuleInfo module, MethodDef method, out MDToken token)
        {
            /*  Data-race is not possible here
             *     - the collection is modified only during ModuleLoadFinished event
             *     - once we access mapping data, it is no longer modified */

            token = default;
            if (!externMethodToWrapperReferenceMapping.ContainsKey(module))
                return false;

            return externMethodToWrapperReferenceMapping[module].TryGetValue(method, out token);
        }

        public bool TryGetWrapperFromMethodReference(ModuleInfo module, IMethodDefOrRef method, out MDToken token)
        {
            /*  Data-race is not possible here
             *     - the collection is modified only during ModuleLoadFinished event
             *     - once we access mapping data, it is no longer modified */

            token = default;
            if (!externMethodToWrapperReferenceMapping.ContainsKey(module))
                return false;

            foreach (var (methodDef, wrapperToken) in externMethodToWrapperReferenceMapping[module])
            {
                if (methodDef.FullName != method.FullName)
                    continue;

                token = wrapperToken;
                return true;
            }

            return false;
        }

        public bool TryGetMethodFromWrapperReference(ModuleInfo module, MDToken token, [NotNullWhen(returnValue: true)]  out MethodDef? method)
        {
            /*  Data-race is not possible here
             *     - the collection is modified only during ModuleLoadFinished event
             *     - once we access mapping data, it is no longer modified */

            method = default;
            if (!wrapperReferenceToExternMethodMapping.ContainsKey(module))
                return false;

            return wrapperReferenceToExternMethodMapping[module].TryGetValue(token, out method);
        }
    }
}
