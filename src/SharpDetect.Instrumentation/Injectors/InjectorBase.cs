// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common;
using SharpDetect.Common.Instrumentation;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Instrumentation.Stubs;

namespace SharpDetect.Instrumentation.Injectors
{
    internal abstract class InjectorBase
    {
        protected readonly IModuleBindContext ModuleBindContext;
        protected readonly IMethodDescriptorRegistry MethodDescriptorRegistry;
        internal int ProcessId { get; set; }

        public InjectorBase(IModuleBindContext moduleBindContext, IMethodDescriptorRegistry methodDescriptorRegistry)
        {
            ModuleBindContext = moduleBindContext;
            MethodDescriptorRegistry = methodDescriptorRegistry;
        }

        protected virtual IMethod CreateStub(ModuleDef module, MethodType type, ref IMethod? methodRef)
        {
            if (methodRef is null)
            {
                var identifier = MethodDescriptorRegistry.GetCoreLibraryDescriptor().Methods
                    .SingleOrDefault(record => record.Identifier.IsInjected && record.Identifier.Name == Enum.GetName(typeof(MethodType), type)).Identifier;
                var coreLibModule = ModuleBindContext.GetCoreLibModule(ProcessId);
                var coreLibTypes = coreLibModule.CorLibTypes;
                var typeRef = MetadataGenerator.CreateHelperTypeRef(module);
                var methodSig = MetadataGenerator.GetHelperMethodSig(type, coreLibTypes);
                methodRef = new MemberRefUser(module, identifier.Name, methodSig, typeRef);
            }

            return methodRef;
        }

        public abstract AnalysisEventType? CanInject(MethodDef methodDef, Instruction instruction);
        public abstract void Inject(MethodDef methodDef, int instructionIndex, ulong eventId, UnresolvedMethodStubs stubs);
    }
}
