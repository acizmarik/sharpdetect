// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.SourceLinks;
using SharpDetect.Instrumentation.Injectors.InstructionInjectors;
using SharpDetect.Instrumentation.Injectors.MethodInjectors;
using SharpDetect.Instrumentation.Stubs;

namespace SharpDetect.Instrumentation
{
    internal class CodeInjector
    {
        private readonly IEventDescriptorRegistry eventRegistry;
        private readonly InstructionInjectorBase[] instructionInjectors;
        private readonly MethodInjectorBase[] methodInjectors;

        public CodeInjector(
            int processId, 
            InstructionInjectorBase[] instructionInjectors,
            MethodInjectorBase[] methodInjectors,
            IEventDescriptorRegistry eventRegistry)
        {
            this.instructionInjectors = instructionInjectors;
            this.methodInjectors = methodInjectors;
            this.eventRegistry = eventRegistry;

            foreach (var injector in instructionInjectors)
                injector.ProcessId = processId;
            foreach (var injector in methodInjectors)
                injector.ProcessId = processId;
        }

        public bool HasMethodInjector(MethodDef methodDef)
            => methodInjectors.Any(i => i.CanInject(methodDef) != null);

        public bool HasInstructionInjector(Instruction instruction)
            => instructionInjectors.Any(i => i.CanInject(instruction) != null);

        public bool TryInject(MethodDef method, UnresolvedMethodStubs stubs)
        {
            var first = TryInjectInstructions(method, stubs);
            var second = TryInjectMethod(method, stubs);
            return first || second;
        }

        private bool TryInjectInstructions(MethodDef method, UnresolvedMethodStubs stubs)
        {
            var toInject = new Queue<(Instruction, InstructionInjectorBase, SourceLink)>();
            var sequencePoint = null as dnlib.DotNet.Pdb.SequencePoint;

            // Iterate through all instruction injectors
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.SequencePoint != null)
                    sequencePoint = instruction.SequencePoint;

                // Check all registered instruction injectors
                foreach (var injector in instructionInjectors)
                {
                    var eventType = injector.CanInject(instruction);
                    if (eventType.HasValue)
                    {
                        var sourceLink = eventRegistry.Create(eventType.Value, method, instruction, sequencePoint);
                        toInject.Enqueue((instruction, injector, sourceLink));
                    }
                }
            }

            // Inject all found events
            foreach (var (instruction, injector, sourceLink) in toInject)
            {
                var index = method.Body.Instructions.IndexOf(instruction);
                injector.Inject(method, index, sourceLink.Id, stubs);
            }

            return toInject.Count != 0;
        }

        private bool TryInjectMethod(MethodDef method, UnresolvedMethodStubs stubs)
        {
            var toInject = new Queue<(MethodInjectorBase, SourceLink)>();
            var sequencePoint = null as dnlib.DotNet.Pdb.SequencePoint;

            // Check all registered method injectors
            foreach (var injector in methodInjectors)
            {
                var eventType = injector.CanInject(method);
                if (eventType.HasValue)
                {
                    var sourceLink = eventRegistry.Create(eventType.Value, method, null, sequencePoint);
                    toInject.Enqueue((injector, sourceLink));
                }
            }

            // Inject all found events
            foreach (var (injector, sourceLink) in toInject)
            {
                injector.Inject(method, sourceLink.Id, stubs);
            }

            return toInject.Count != 0;
        }
    }
}
