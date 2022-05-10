using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Common.SourceLinks;
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

        public abstract AnalysisEventType? CanInject(MethodDef methodDef, Instruction instruction);
        public abstract void Inject(MethodDef methodDef, int instructionIndex, ulong eventId, UnresolvedMethodStubs stubs);
    }
}
