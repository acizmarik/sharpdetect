using Microsoft.Extensions.Configuration;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services;

namespace SharpDetect.Core.Plugins
{
    internal class PluginsProxy : IDisposable
    {
        private readonly IPluginsManager pluginsManager;
        private readonly IShadowExecutionObserver runtimeEventsHub;
        private readonly IPlugin[] plugins;
        private bool isDisposed;

        public PluginsProxy(IConfiguration configuration, IPluginsManager pluginsManager, IShadowExecutionObserver runtimeEventsHub)
        {
            this.pluginsManager = pluginsManager;
            this.runtimeEventsHub = runtimeEventsHub;
            var chain = configuration[Constants.Configuration.PluginsChain].Split('|');

            Guard.True<ArgumentException>(pluginsManager.TryConstructPlugins(chain, out plugins));
        }

        public void Initialize()
        {
            runtimeEventsHub.ProfilerInitialized += RuntimeEventsHub_ProfilerInitialized;
            runtimeEventsHub.ProfilerDestroyed += RuntimeEventsHub_ProfilerDestroyed;
            runtimeEventsHub.ModuleLoaded += RuntimeEventsHub_ModuleLoaded;
            runtimeEventsHub.TypeLoaded += RuntimeEventsHub_TypeLoaded;
            runtimeEventsHub.JITCompilationStarted += RuntimeEventsHub_JITCompilationStarted;
            runtimeEventsHub.ThreadCreated += RuntimeEventsHub_ThreadCreated;
            runtimeEventsHub.ThreadDestroyed += RuntimeEventsHub_ThreadDestroyed;
            runtimeEventsHub.MethodCalled += RuntimeEventsHub_MethodCalled;
            runtimeEventsHub.MethodReturned += RuntimeEventsHub_MethodReturned;
            runtimeEventsHub.LockAcquireAttempted += RuntimeEventsHub_LockAcquireAttempted;
            runtimeEventsHub.LockAcquireReturned += RuntimeEventsHub_LockAcquireReturned;
            runtimeEventsHub.LockReleaseReturned += RuntimeEventsHub_LockReleaseReturned;
            runtimeEventsHub.ObjectWaitAttempted += RuntimeEventsHub_ObjectWaitAttempted;
            runtimeEventsHub.ObjectWaitReturned += RuntimeEventsHub_ObjectWaitReturned;
            runtimeEventsHub.ObjectPulseReturned += RuntimeEventsHub_ObjectPulseReturned;
            runtimeEventsHub.GarbageCollectionStarted += RuntimeEventsHub_GarbageCollectionStarted;
            runtimeEventsHub.GarbageCollectionFinished += RuntimeEventsHub_GarbageCollectionFinished;
        }

        private void RuntimeEventsHub_ProfilerInitialized((IShadowCLR Runtime, EventInfo Info) obj) => Execute(plugin => plugin.AnalysisStarted(obj.Info));
        private void RuntimeEventsHub_ProfilerDestroyed((IShadowCLR Runtime, EventInfo Info) obj) => Execute(plugin => plugin.AnalysisEnded(obj.Info));
        private void RuntimeEventsHub_ModuleLoaded((IShadowCLR Runtime, ModuleInfo Module, string Path, EventInfo Info) obj) => Execute(plugin => plugin.ModuleLoaded(obj.Module, obj.Path, obj.Info));
        private void RuntimeEventsHub_TypeLoaded((IShadowCLR Runtime, TypeInfo Type, EventInfo Info) obj) => Execute(plugin => plugin.TypeLoaded(obj.Type, obj.Info));
        private void RuntimeEventsHub_JITCompilationStarted((IShadowCLR Runtime, FunctionInfo Function, EventInfo Info) obj) => Execute(plugin => plugin.JITCompilationStarted(obj.Function, obj.Info));
        private void RuntimeEventsHub_ThreadCreated((IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info) obj) => Execute(plugin => plugin.ThreadCreated(obj.ThreadId, obj.Info));
        private void RuntimeEventsHub_ThreadDestroyed((IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info) obj) => Execute(plugin => plugin.ThreadDestroyed(obj.ThreadId, obj.Info));
        private void RuntimeEventsHub_MethodCalled((IShadowCLR Runtime, FunctionInfo Function, Common.Runtime.Arguments.IArgumentsList? Arguments, EventInfo Info) obj) => Execute(plugin => plugin.MethodCalled(obj.Function, obj.Arguments, obj.Info));
        private void RuntimeEventsHub_MethodReturned((IShadowCLR Runtime, FunctionInfo Function, Common.Runtime.Arguments.IValueOrObject? returnValue, Common.Runtime.Arguments.IArgumentsList? ByRefArguments, EventInfo Info) obj) => Execute(plugin => plugin.MethodReturned(obj.Function, obj.returnValue, obj.ByRefArguments, obj.Info));
        private void RuntimeEventsHub_LockAcquireAttempted((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info) obj) => Execute(plugin => plugin.LockAcquireAttempted(obj.Instance, obj.Info));
        private void RuntimeEventsHub_LockAcquireReturned((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, EventInfo Info) obj) => Execute(plugin => plugin.LockAcquireReturned(obj.Instance, obj.IsSuccess, obj.Info));
        private void RuntimeEventsHub_LockReleaseReturned((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info) obj) => Execute(plugin => plugin.LockReleased(obj.Instance, obj.Info));
        private void RuntimeEventsHub_ObjectWaitAttempted((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info) obj) => Execute(plugin => plugin.ObjectWaitCalled(obj.Instance, obj.Info));
        private void RuntimeEventsHub_ObjectWaitReturned((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, EventInfo Info) obj) => Execute(plugin => plugin.ObjectWaitReturned(obj.Instance, obj.IsSuccess, obj.Info));
        private void RuntimeEventsHub_ObjectPulseReturned((IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, EventInfo Info) obj) => Execute(plugin => plugin.ObjectPulsed(obj.Instance, obj.IsPulseAll, obj.Info));
        private void RuntimeEventsHub_GarbageCollectionStarted((IShadowCLR Runtime, bool[] Generations, Common.Interop.COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info) obj) => Execute(plugin => plugin.GarbageCollectionStarted(obj.Info));
        private void RuntimeEventsHub_GarbageCollectionFinished((IShadowCLR Runtime, Common.Interop.COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info) obj) => Execute(plugin => plugin.GarbageCollectionFinished(obj.Info));

        public void Dispose()
        {
            if (!isDisposed)
            {
                runtimeEventsHub.ProfilerInitialized -= RuntimeEventsHub_ProfilerInitialized;
                runtimeEventsHub.ProfilerDestroyed -= RuntimeEventsHub_ProfilerDestroyed;
                runtimeEventsHub.ModuleLoaded -= RuntimeEventsHub_ModuleLoaded;
                runtimeEventsHub.TypeLoaded -= RuntimeEventsHub_TypeLoaded;
                runtimeEventsHub.JITCompilationStarted -= RuntimeEventsHub_JITCompilationStarted;
                runtimeEventsHub.ThreadCreated -= RuntimeEventsHub_ThreadCreated;
                runtimeEventsHub.ThreadDestroyed -= RuntimeEventsHub_ThreadDestroyed;
                runtimeEventsHub.MethodCalled -= RuntimeEventsHub_MethodCalled;
                runtimeEventsHub.MethodReturned -= RuntimeEventsHub_MethodReturned;
                runtimeEventsHub.LockAcquireAttempted -= RuntimeEventsHub_LockAcquireAttempted;
                runtimeEventsHub.LockAcquireReturned -= RuntimeEventsHub_LockAcquireReturned;
                runtimeEventsHub.LockReleaseReturned -= RuntimeEventsHub_LockReleaseReturned;
                runtimeEventsHub.ObjectWaitAttempted -= RuntimeEventsHub_ObjectWaitAttempted;
                runtimeEventsHub.ObjectWaitReturned -= RuntimeEventsHub_ObjectWaitReturned;
                runtimeEventsHub.ObjectPulseReturned -= RuntimeEventsHub_ObjectPulseReturned;
                runtimeEventsHub.GarbageCollectionStarted -= RuntimeEventsHub_GarbageCollectionStarted;
                runtimeEventsHub.GarbageCollectionFinished -= RuntimeEventsHub_GarbageCollectionFinished;

                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public void Execute(Action<IPlugin> action)
        {
            foreach (var plugin in plugins)
                action(plugin);
        }
    }
}
