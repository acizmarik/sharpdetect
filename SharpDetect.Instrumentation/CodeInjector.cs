using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common;
using SharpDetect.Common.Services.Instrumentation;
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
            var toInject = new Queue<(Instruction, int, AnalysisEventType, InjectorBase)>();

            // Iterate through all instructions
            var counter = 0;
            foreach (var instruction in method.Body.Instructions)
            {
                // Check all registered injectors
                foreach (var injector in injectors)
                {
                    var eventType = injector.CanInject(method, instruction);
                    if (eventType.HasValue)
                    {
                        toInject.Enqueue((instruction, counter, eventType.Value, injector));
                    }
                }

                counter++;
            }

            // Inject all found events
            foreach (var (instruction, index, eventType, injector) in toInject)
            {
                var sourceLink = eventRegistry.Create(eventType, method, instruction.Offset, instruction.GetSequencePoint());
                injector.Inject(method, index, sourceLink.Id, stubs);
            }

            return toInject.Count != 0;
        }
    }
}
