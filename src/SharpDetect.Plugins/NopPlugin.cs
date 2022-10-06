using Microsoft.Extensions.Configuration;
using SharpDetect.Common;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Runtime.Threads;

namespace SharpDetect.Plugins
{
    [PluginExport("Nop", "1.0.0")]
    public class NopPlugin : IPlugin
    {
        public virtual void Configure(IConfiguration configuration) { /* Intentionally empty */ }
        public virtual void AnalysisEnded(EventInfo info) { /* Intentionally empty */ }
        public virtual void AnalysisStarted(EventInfo info) { /* Intentionally empty */ }
        public virtual void ArrayElementRead(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) { /* Intentionally empty */ }
        public virtual void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) { /* Intentionally empty */ }
        public virtual void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info) { /* Intentionally empty */ }
        public virtual void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info) { /* Intentionally empty */ }
        public virtual void GarbageCollectionFinished(EventInfo info) { /* Intentionally empty */ }
        public virtual void GarbageCollectionStarted(EventInfo info) { /* Intentionally empty */ }
        public virtual void JITCompilationStarted(FunctionInfo method, EventInfo info) { /* Intentionally empty */ }
        public virtual void LockAcquireAttempted(IShadowObject instance, EventInfo info) { /* Intentionally empty */ }
        public virtual void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info) { /* Intentionally empty */ }
        public virtual void LockReleased(IShadowObject instance, EventInfo info) { /* Intentionally empty */ }
        public virtual void MethodCalled(FunctionInfo method, IArgumentsList? arguments, EventInfo info) { /* Intentionally empty */ }
        public virtual void MethodReturned(FunctionInfo method, IValueOrObject? returnValue, IArgumentsList? byRefArguments, EventInfo info) { /* Intentionally empty */ }
        public virtual void ModuleLoaded(ModuleInfo module, string path, EventInfo info) { /* Intentionally empty */ }
        public virtual void ObjectPulsed(IShadowObject instance, bool isPulseAll, EventInfo info) { /* Intentionally empty */ }
        public virtual void ObjectWaitCalled(IShadowObject instance, EventInfo info) { /* Intentionally empty */ }
        public virtual void ObjectWaitReturned(IShadowObject instance, bool isSuccess, EventInfo info) { /* Intentionally empty */ }
        public virtual void ThreadCreated(IShadowThread thread, EventInfo info) { /* Intentionally empty */ }
        public virtual void ThreadDestroyed(IShadowThread thread, EventInfo info) { /* Intentionally empty */ }
        public virtual void TypeLoaded(TypeInfo type, EventInfo info) { /* Intentionally empty */ }
    }
}
