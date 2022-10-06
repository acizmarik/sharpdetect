using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Core.Runtime;
using System.Runtime.CompilerServices;

namespace SharpDetect.Core.Plugins
{
    internal class PluginsProxy : IDisposable
    {
        private readonly IConfiguration configuration;
        private readonly IShadowExecutionObserver runtimeEventsHub;
        private readonly IServiceProvider serviceProvider;
        private readonly IPluginsManager pluginsManager;
        private readonly ILogger<PluginsProxy> logger;
        private IPlugin[] plugins;
        private bool isDisposed;

        public PluginsProxy(
            IConfiguration configuration, 
            IServiceProvider serviceProvider, 
            IPluginsManager pluginsManager, 
            IShadowExecutionObserver runtimeEventsHub,
            ILoggerFactory loggerFactory)
        {
            this.configuration = configuration;
            this.serviceProvider = serviceProvider;
            this.runtimeEventsHub = runtimeEventsHub;
            this.pluginsManager = pluginsManager;
            this.plugins = null!;
            this.logger = loggerFactory.CreateLogger<PluginsProxy>();
        }

        public void Initialize()
        {
            var pluginIdentifiers = configuration[Constants.Configuration.PluginsChain].Split('|');
            var serviceProvider = CreatePluginServiceProvider();
            Guard.True<ArgumentException>(pluginsManager.TryConstructPlugins(pluginIdentifiers, configuration, serviceProvider, out plugins));

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
            runtimeEventsHub.FieldAccessed += RuntimeEventsHub_FieldAccessed;
            runtimeEventsHub.ArrayElementAccessed += RuntimeEventsHub_ArrayElementAccessed;
        }

        private IServiceProvider CreatePluginServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(serviceProvider.GetRequiredService<ILoggerFactory>());
            serviceCollection.AddSingleton(serviceProvider.GetRequiredService<IMetadataContext>());
            serviceCollection.AddSingleton(serviceProvider.GetRequiredService<IReportingService>());
            serviceCollection.AddSingleton(serviceProvider.GetRequiredService<IEventDescriptorRegistry>());
            return serviceCollection.BuildServiceProvider();
        }

        private void RuntimeEventsHub_ProfilerInitialized((IShadowCLR Runtime, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.AnalysisStarted(CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_ProfilerDestroyed((IShadowCLR Runtime, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.AnalysisEnded(CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_ModuleLoaded((IShadowCLR Runtime, ModuleInfo Module, string Path, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.ModuleLoaded(obj.Module, obj.Path, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_TypeLoaded((IShadowCLR Runtime, TypeInfo Type, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.TypeLoaded(obj.Type, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_JITCompilationStarted((IShadowCLR Runtime, FunctionInfo Function, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.JITCompilationStarted(obj.Function, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_ThreadCreated((IShadowCLR Runtime, UIntPtr ThreadId, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.ThreadCreated(((ShadowCLR)obj.Runtime).Threads[obj.Info.ThreadId], CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_ThreadDestroyed((IShadowCLR Runtime, UIntPtr ThreadId, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.ThreadDestroyed(((ShadowCLR)obj.Runtime).Threads[obj.Info.ThreadId], CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_MethodCalled((IShadowCLR Runtime, FunctionInfo Function, Common.Runtime.Arguments.IArgumentsList? Arguments, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.MethodCalled(obj.Function, obj.Arguments, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_MethodReturned((IShadowCLR Runtime, FunctionInfo Function, Common.Runtime.Arguments.IValueOrObject? returnValue, Common.Runtime.Arguments.IArgumentsList? ByRefArguments, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.MethodReturned(obj.Function, obj.returnValue, obj.ByRefArguments, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_LockAcquireAttempted((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.LockAcquireAttempted(obj.Instance, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_LockAcquireReturned((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.LockAcquireReturned(obj.Instance, obj.IsSuccess, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_LockReleaseReturned((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.LockReleased(obj.Instance, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_ObjectWaitAttempted((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.ObjectWaitCalled(obj.Instance, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_ObjectWaitReturned((IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.ObjectWaitReturned(obj.Instance, obj.IsSuccess, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_ObjectPulseReturned((IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.ObjectPulsed(obj.Instance, obj.IsPulseAll, CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_GarbageCollectionStarted((IShadowCLR Runtime, bool[] Generations, Common.Interop.COR_PRF_GC_GENERATION_RANGE[] Bounds, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.GarbageCollectionStarted(CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_GarbageCollectionFinished((IShadowCLR Runtime, Common.Interop.COR_PRF_GC_GENERATION_RANGE[] Bounds, Common.RawEventInfo Info) obj) => Execute(plugin => plugin.GarbageCollectionFinished(CreatePluginEventInfo(obj.Runtime, obj.Info)));
        private void RuntimeEventsHub_FieldAccessed((IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject? Instance, Common.RawEventInfo Info) obj) => Execute(plugin =>
        {
            if (obj.IsWrite)
                plugin.FieldWritten(obj.Identifier, obj.Instance, false, CreatePluginEventInfo(obj.Runtime, obj.Info));
            else
                plugin.FieldRead(obj.Identifier, obj.Instance, false, CreatePluginEventInfo(obj.Runtime, obj.Info));
        });
        private void RuntimeEventsHub_ArrayElementAccessed((IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject Instance, int Index, Common.RawEventInfo Info) obj) => Execute(plugin =>
        {
            if (obj.IsWrite)
                plugin.ArrayElementWritten(obj.Identifier, obj.Instance, obj.Index, CreatePluginEventInfo(obj.Runtime, obj.Info));
            else
                plugin.ArrayElementRead(obj.Identifier, obj.Instance, obj.Index, CreatePluginEventInfo(obj.Runtime, obj.Info));
        });

        private static EventInfo CreatePluginEventInfo(IShadowCLR shadowRuntime, Common.RawEventInfo info)
        {
            return new(shadowRuntime, ((ShadowCLR)shadowRuntime).Threads[info.ThreadId]);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                // Release event handlers
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
                runtimeEventsHub.FieldAccessed -= RuntimeEventsHub_FieldAccessed;

                // Dispose plugins that are implemented as IDisposable
                foreach (var plugin in plugins)
                {
                    if (plugin.GetType().IsAssignableTo(typeof(IDisposable)))
                        ((IDisposable)plugin)!.Dispose();
                }

                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public void Execute(Action<IPlugin> action, [CallerMemberName] string? memberName = null)
        {
            IPlugin? currentPlugin = null; 
            foreach (var plugin in plugins)
            {
                try
                {
                    currentPlugin = plugin;
                    action(plugin);
                }
                catch (Exception ex)
                {
                    pluginsManager.TryGetPluginInfo(currentPlugin!, out var pluginInfo);
                    logger.LogWarning(ex, "[{class}] An unhandled exception occurred in plugin {plugin} while executing {event}.",
                        nameof(PluginsProxy), (pluginInfo.HasValue) ? pluginInfo.Value.Name : "<unable-to-resolve-plugin>", memberName);
                }
            }
        }
    }
}
