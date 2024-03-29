﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Services.Descriptors;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Core.Models
{
    public class MethodDescriptorRegistry : IMethodDescriptorRegistry
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<MethodIdentifier, MethodInterpretationData>> unresolvedLookup;
        private readonly ConcurrentDictionary<MethodDef, MethodInterpretationData> resolvedLookup;
        private ILibraryDescriptor coreLibraryDescriptor;

        public MethodDescriptorRegistry()
        {
            unresolvedLookup = new();
            resolvedLookup = new();
            coreLibraryDescriptor = null!;
        }

        public void Register(ILibraryDescriptor library)
        {
            var unresolvedLibraryRecords = new ConcurrentDictionary<MethodIdentifier, MethodInterpretationData>();
            foreach (var (identifier, interpretationData) in library.Methods)
                unresolvedLibraryRecords.TryAdd(identifier, interpretationData);

            unresolvedLookup.TryAdd(library.AssemblyName, unresolvedLibraryRecords);

            // Check if the library is CoreLib
            if (library.IsCoreLibrary)
                coreLibraryDescriptor = library;
        }

        public void Register((MethodIdentifier Identifier, MethodInterpretationData Interpretation) method, string assemblyName)
        {
            unresolvedLookup.TryAdd(assemblyName, new());
            unresolvedLookup[assemblyName].TryAdd(method.Identifier, method.Interpretation);
        }

        public ILibraryDescriptor GetCoreLibraryDescriptor()
        {
            return coreLibraryDescriptor;
        }

        public IEnumerable<string> GetSupportedLibraries()
        {
            foreach (var (name, _) in unresolvedLookup)
                yield return name;
        }

        public bool TryGetMethodInterpretationData(MethodDef method, [NotNullWhen(returnValue: true)] out MethodInterpretationData? data)
        {
            // Try to search in resolved first
            if (resolvedLookup.TryGetValue(method, out data))
            {
                // True only if the record is usable
                return !data.IsEmpty();
            }

            // Try match an unresolved record
            var assemblyName = (method.Module != null) ? method.Module.Assembly.Name.String : coreLibraryDescriptor.AssemblyName;
            if (!unresolvedLookup.TryGetValue(assemblyName, out var unresolvedAssemblyRecords))
            {
                // There are no records for the given assembly
                return false;
            }
            
            var identifier = new MethodIdentifier(
                method.Name,
                method.DeclaringType.FullName,
                method.IsStatic,
                (ushort)method.Parameters.Count,
                new(method.Parameters.Select(p => p.Type.FullName).ToList()),
                method is MethodDefUser);
            if (!unresolvedAssemblyRecords.TryGetValue(identifier, out data))
            {
                // There are no records for the given assembly
                // Create cache record for this computed value
                resolvedLookup.TryAdd(method, MethodInterpretationData.CreateEmpty());
                return false;
            }

            // We successfully resolved an identifier to MethodDef
            // Create cache record for this computed value
            resolvedLookup.TryAdd(method, data);
            return true;
        }

        public IEnumerable<(MethodIdentifier Identifier, MethodInterpretationData Data)> GetRegisteredMethods(string assemblyName)
        {
            if (!unresolvedLookup.ContainsKey(assemblyName))
                yield break;

            foreach (var method in unresolvedLookup[assemblyName])
                yield return (method.Key, method.Value);
        }
    }
}
