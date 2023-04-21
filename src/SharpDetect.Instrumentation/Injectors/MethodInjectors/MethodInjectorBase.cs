using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Instrumentation.Stubs;

namespace SharpDetect.Instrumentation.Injectors.MethodInjectors
{
    internal abstract class MethodInjectorBase : InjectorBase
    {
        public MethodInjectorBase(
            IModuleBindContext moduleBindContext,
            IMethodDescriptorRegistry methodDescriptorRegistry)
            : base(moduleBindContext, methodDescriptorRegistry)
        {

        }

        public abstract AnalysisEventType? CanInject(MethodDef methodDef);
        public abstract void Inject(MethodDef methodDef, ulong eventId, UnresolvedMethodStubs stubs);
    }
}
