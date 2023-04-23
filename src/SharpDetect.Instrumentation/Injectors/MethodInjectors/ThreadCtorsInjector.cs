// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Instrumentation.Stubs;
using SharpDetect.Instrumentation.Utilities;

namespace SharpDetect.Instrumentation.Injectors.MethodInjectors
{
    internal class ThreadCtorsInjector : MethodInjectorBase
    {
        private IMethod? dummyThreadAllocatedRef;

        public ThreadCtorsInjector(
            IModuleBindContext moduleBindContext,
            IMethodDescriptorRegistry methodDescriptorRegistry)
            : base(moduleBindContext, methodDescriptorRegistry)
        {
        }

        public override AnalysisEventType? CanInject(MethodDef methodDef)
        {
            return methodDef.IsInstanceConstructor &&
                   methodDef.DeclaringType.Name.Equals("Thread") &&
                   methodDef.DeclaringType.Namespace.Equals("System.Threading")
                   ? AnalysisEventType.ThreadAllocation : null;
        }

        public override void Inject(MethodDef methodDef, ulong eventId, UnresolvedMethodStubs stubs)
        {
            var stub = CreateStub(methodDef.Module, MethodType.ArrayElementAccess, ref dummyThreadAllocatedRef);
            var threadAllocatedInstruction = Instruction.Create(OpCodes.Call, stub);
            var nativeThreadHandleField = methodDef.DeclaringType.FindField("_DONT_USE_InternalThread");
            RuntimeContract.Assert(nativeThreadHandleField != null);
            var lastInstruction = methodDef.Body.Instructions[methodDef.Body.Instructions.Count - 1];

            methodDef.InjectBefore(lastInstruction, new[]
            {
                // Load 'this' (current thread instance)
                Instruction.Create(OpCodes.Ldarg_0),
                // Load thread's native handle from magical field '_DONT_USE_InternalThread'
                Instruction.Create(OpCodes.Ldfld, nativeThreadHandleField),
                // Call 
                threadAllocatedInstruction
            });

            stubs.Add(threadAllocatedInstruction, HelperOrWrapperReferenceStub.CreateHelperMethodReferenceStub(MethodType.ThreadAllocation));
        }
    }
}
