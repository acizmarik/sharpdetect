using SharpDetect.Common;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;

namespace SharpDetect.Plugins
{
    [PluginExport("Empty", "1.0.0")]
    public class EmptyPlugin : IPlugin
    {
        public void AnalysisEnded(EventInfo info) { /* Intentionally empty */ }
        public void AnalysisStarted(EventInfo info) { /* Intentionally empty */ }
        public void ArrayElementRead(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) { /* Intentionally empty */ }
        public void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) { /* Intentionally empty */ }
        public void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info) { /* Intentionally empty */ }
        public void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info) { /* Intentionally empty */ }
        public void GarbageCollectionFinished(EventInfo info) { /* Intentionally empty */ }
        public void GarbageCollectionStarted(EventInfo info) { /* Intentionally empty */ }
        public void Initialize(IServiceProvider serviceProvider) { /* Intentionally empty */ }
        public void JITCompilationStarted(FunctionInfo method, EventInfo info) { /* Intentionally empty */ }
        public void LockAcquireAttempted(IShadowObject instance, EventInfo info) { /* Intentionally empty */ }
        public void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info) { /* Intentionally empty */ }
        public void LockReleased(IShadowObject instance, EventInfo info) { /* Intentionally empty */ }
        public void MethodCalled(FunctionInfo method, IArgumentsList? arguments, EventInfo info) { /* Intentionally empty */ }
        public void MethodReturned(FunctionInfo method, IValueOrObject? returnValue, IArgumentsList? byRefArguments, EventInfo info) { /* Intentionally empty */ }
        public void ModuleLoaded(ModuleInfo module, string path, EventInfo info) { /* Intentionally empty */ }
        public void ObjectPulsed(IShadowObject instance, bool isPulseAll, EventInfo info) { /* Intentionally empty */ }
        public void ObjectWaitCalled(IShadowObject instance, EventInfo info) { /* Intentionally empty */ }
        public void ObjectWaitReturned(IShadowObject instance, bool isSuccess, EventInfo info) { /* Intentionally empty */ }
        public void ThreadCreated(UIntPtr threadId, EventInfo info) { /* Intentionally empty */ }
        public void ThreadDestroyed(UIntPtr threadId, EventInfo info) { /* Intentionally empty */ }
        public void TypeLoaded(TypeInfo type, EventInfo info) { /* Intentionally empty */ }
    }
}
