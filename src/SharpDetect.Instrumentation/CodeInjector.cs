using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.SourceLinks;
using SharpDetect.Instrumentation.Injectors;
using SharpDetect.Instrumentation.Stubs;

namespace SharpDetect.Instrumentation
{
    internal class CodeInjector
    {
        private readonly IEventDescriptorRegistry eventRegistry;
        private readonly InjectorBase[] injectors;

        public CodeInjector(int processId, InjectorBase[] injectors, IEventDescriptorRegistry eventRegistry)
        {
            this.injectors = injectors;
            this.eventRegistry = eventRegistry;

            foreach (var injector in injectors)
                injector.ProcessId = processId;
        }

        public bool TryInject(MethodDef method, UnresolvedMethodStubs stubs)
        {
            var toInject = new Queue<(Instruction, InjectorBase, SourceLink)>();
            var sequencePoint = null as dnlib.DotNet.Pdb.SequencePoint;

            // Iterate through all instructions
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.SequencePoint != null)
                    sequencePoint = instruction.SequencePoint;

                // Check all registered injectors
                foreach (var injector in injectors)
                {
                    var eventType = injector.CanInject(method, instruction);
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
    }
}
