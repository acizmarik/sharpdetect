using Microsoft.Extensions.Configuration;
using SharpDetect.Common;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;

namespace SharpDetect.Plugins
{
    [PluginExport("Echo", "1.0.0")]
    public class EchoPlugin : IPlugin
    {
        public void Initialize(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        public void AnalysisEnded(EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void AnalysisStarted(EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void ArrayElementRead(ulong srcMappingId, IShadowObject instance, int index, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void GarbageCollectionFinished(EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void GarbageCollectionStarted(EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void JITCompilationStarted(FunctionInfo method, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void LockAcquireAttempted(IShadowObject instance, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void LockReleased(IShadowObject instance, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void MethodCalled(FunctionInfo method, IArgumentsList? arguments, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void MethodReturned(FunctionInfo method, IValueOrObject? returnValue, IArgumentsList? byRefArguments, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void ModuleLoaded(ModuleInfo module, string path, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void ObjectPulsed(IShadowObject instance, bool isPulseAll, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void ObjectWaitCalled(IShadowObject instance, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void ObjectWaitReturned(IShadowObject instance, bool isSuccess, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void ThreadCreated(UIntPtr threadId, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void ThreadDestroyed(UIntPtr threadId, EventInfo info)
        {
            throw new NotImplementedException();
        }

        public void TypeLoaded(TypeInfo type, EventInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
