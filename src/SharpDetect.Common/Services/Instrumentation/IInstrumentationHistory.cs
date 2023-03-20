// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;

namespace SharpDetect.Common.Services.Instrumentation
{
    public interface IInstrumentationHistory
    {
        bool Enabled { get; }

        IEnumerable<TypeDef> GetAllInjectedTypesForAssembly(AssemblyDef assembly);
        IEnumerable<MethodDef> GetAllInjectedMethodsForAssembly(AssemblyDef assembly);
        void MarkAsDirty(AssemblyDef assemblyDef);
        void ApplyChanges();
        void SaveAssemblies();
    }
}
