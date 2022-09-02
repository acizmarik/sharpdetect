using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Runtime.Threads;
using System.Collections.Concurrent;

namespace SharpDetect.IntegrationTests.Mocks
{
    [PluginExport("Internal-TestPlugin", "1.0.0")]
    public class MockPlugin : IPlugin
    {
        private BlockingCollection<string> sink;

        public MockPlugin()
        {
            sink = null!;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            sink = serviceProvider.GetRequiredService<BlockingCollection<string>>();
        }

        public void AnalysisEnded(EventInfo info) => sink.Add(nameof(AnalysisEnded));
        public void AnalysisStarted(EventInfo info) => sink.Add(nameof(AnalysisStarted));
        public void ArrayElementRead(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) => sink.Add(nameof(ArrayElementRead));
        public void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info) => sink.Add(nameof(ArrayElementWritten));
        public void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info) => sink.Add(nameof(FieldRead));
        public void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info) => sink.Add(nameof(FieldWritten));
        public void GarbageCollectionFinished(EventInfo info) => sink.Add(nameof(GarbageCollectionFinished));
        public void GarbageCollectionStarted(EventInfo info) => sink.Add(nameof(GarbageCollectionStarted));
        public void JITCompilationStarted(FunctionInfo method, EventInfo info) => sink.Add(nameof(JITCompilationStarted));
        public void LockAcquireAttempted(IShadowObject instance, EventInfo info) => sink.Add(nameof(LockAcquireAttempted));
        public void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info) => sink.Add(nameof(LockAcquireReturned));
        public void LockReleased(IShadowObject instance, EventInfo info) => sink.Add(nameof(LockReleased));
        public void MethodCalled(FunctionInfo method, IArgumentsList? arguments, EventInfo info) => sink.Add(nameof(MethodCalled));
        public void MethodReturned(FunctionInfo method, IValueOrObject? returnValue, IArgumentsList? byRefArguments, EventInfo info) => sink.Add(nameof(MethodReturned));
        public void ModuleLoaded(ModuleInfo module, string path, EventInfo info) => sink.Add(nameof(ModuleLoaded));
        public void ObjectPulsed(IShadowObject instance, bool isPulseAll, EventInfo info) => sink.Add(nameof(ObjectPulsed));
        public void ObjectWaitCalled(IShadowObject instance, EventInfo info) => sink.Add(nameof(ObjectWaitCalled));
        public void ObjectWaitReturned(IShadowObject instance, bool isSuccess, EventInfo info) => sink.Add(nameof(ObjectWaitReturned));
        public void ThreadCreated(IShadowThread thread, EventInfo info) => sink.Add(nameof(ThreadCreated));
        public void ThreadDestroyed(IShadowThread thread, EventInfo info) => sink.Add(nameof(ThreadDestroyed));
        public void TypeLoaded(TypeInfo type, EventInfo info) => sink.Add(nameof(TypeLoaded));

        public void Dispose()
        {
            /* Do nothing */
        }
    }
}
