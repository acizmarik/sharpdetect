using System.Collections.Immutable;

namespace SharpDetect.Profiler
{
    internal record InstrumentationContext
    {
        public record struct InjectedHelperMethodInfo(string Name, MdMethodDef Token, COR_SIGNATURE[] Signature);

        public readonly ICorProfilerInfo CorProfilerInfo;
        public readonly Assembly CoreAssembly;
        public readonly Module CoreModule;
        public readonly List<InjectedHelperMethodInfo> HelperMethods;

        public MdTypeDef EventDispatcherTypeDef { get; init; }

        public InstrumentationContext(ICorProfilerInfo corProfilerInfo, Assembly coreAssembly, Module coreModule)
        {
            CorProfilerInfo = corProfilerInfo;
            CoreAssembly = coreAssembly;
            CoreModule = coreModule;
            HelperMethods = new();
        }
    }
}
