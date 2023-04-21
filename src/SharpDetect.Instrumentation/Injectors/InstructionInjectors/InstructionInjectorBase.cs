using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Instrumentation.Stubs;

namespace SharpDetect.Instrumentation.Injectors.InstructionInjectors
{
    internal abstract class InstructionInjectorBase : InjectorBase
    {
        public InstructionInjectorBase(
            IModuleBindContext moduleBindContext,
            IMethodDescriptorRegistry methodDescriptorRegistry)
            : base(moduleBindContext, methodDescriptorRegistry)
        {

        }

        public abstract AnalysisEventType? CanInject(Instruction instruction);
        public abstract void Inject(MethodDef methodDef, int instructionIndex, ulong eventId, UnresolvedMethodStubs stubs);
    }
}
